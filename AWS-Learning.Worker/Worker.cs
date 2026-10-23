using AWS_Learning.Models;
using AWS_Learning.Services;
using System.Text.Json;

namespace AWS_Learning.Worker;

public sealed class Worker(
    IServiceScopeFactory scopeFactory,
    ILogger<Worker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Document worker started. Receiving one message at a time.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var queue = scope.ServiceProvider.GetRequiredService<IQueueService>();
                var messages = await queue.ReceiveAsync(1, stoppingToken);

                foreach (var message in messages)
                {
                    await ProcessDeliveryAsync(message, queue, scope.ServiceProvider, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Queue receive failed. Retrying in three seconds.");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task ProcessDeliveryAsync(
        ReceivedJobMessage message,
        IQueueService queue,
        IServiceProvider services,
        CancellationToken stoppingToken)
    {
        var jobs = services.GetRequiredService<IJobService>();
        var processor = services.GetRequiredService<IProcessingService>();
        string? jobId = null;
        var processingCompleted = false;

        using var visibilityCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var visibilityTask = MaintainVisibilityAsync(queue, message.ReceiptHandle, visibilityCancellation.Token);

        try
        {
            var payload = JsonSerializer.Deserialize<DocumentJobMessage>(message.Body, JsonOptions)
                ?? throw new JsonException("Message body is empty.");
            ArgumentException.ThrowIfNullOrWhiteSpace(payload.JobId);
            jobId = payload.JobId;

            var job = await jobs.GetAsync(jobId, stoppingToken)
                ?? throw new KeyNotFoundException($"Job '{jobId}' does not exist.");

            if (payload.Bucket != job.Bucket || payload.Key != job.SourceKey)
            {
                throw new InvalidDataException("Message source does not match the persisted job.");
            }

            logger.LogInformation("Processing job {JobId}; delivery {MessageId}, receive count {ReceiveCount}",
                jobId, message.MessageId, message.ReceiveCount);

            await processor.ProcessAsync(job, stoppingToken);
            processingCompleted = true;
            await queue.DeleteAsync(message.ReceiptHandle, stoppingToken);
            logger.LogInformation("Job {JobId} completed and message acknowledged.", jobId);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Delivery {MessageId} failed for job {JobId}; message remains unacknowledged.",
                message.MessageId, jobId);

            // An acknowledgement failure must not turn completed processing into Failed.
            if (!processingCompleted && jobId is not null)
            {
                try
                {
                    await jobs.UpdateStatusAsync(jobId, "Failed", null, stoppingToken);
                }
                catch (Exception statusException) when (statusException is not OperationCanceledException)
                {
                    logger.LogWarning(statusException, "Could not record failure for job {JobId}.", jobId);
                }
            }
        }
        finally
        {
            await visibilityCancellation.CancelAsync();
            try
            {
                await visibilityTask;
            }
            catch (OperationCanceledException) when (visibilityCancellation.IsCancellationRequested)
            {
            }
        }
    }

    private async Task MaintainVisibilityAsync(
        IQueueService queue,
        string receiptHandle,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
            try
            {
                await queue.ChangeVisibilityAsync(receiptHandle, 60, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Could not extend message visibility. Duplicate delivery is possible.");
                return;
            }
        }
    }
}
