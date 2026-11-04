using System.Globalization;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS_Learning.Monitoring.Worker.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AWS_Learning.Monitoring.Worker;

public sealed class Worker(
    IAmazonSQS sqs,
    IAmazonCloudWatch cloudWatch,
    IOptions<MonitoringOptions> options,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var interval = TimeSpan.FromSeconds(settings.IntervalSeconds);

        logger.LogInformation(
            "DLQ monitoring started. Collection interval: {IntervalSeconds}s",
            settings.IntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectAndPublishAsync(settings, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Monitoring cycle failed. Retrying after {IntervalSeconds}s.",
                    settings.IntervalSeconds);
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("DLQ monitoring stopped.");
    }

    private async Task CollectAndPublishAsync(
        MonitoringOptions settings,
        CancellationToken cancellationToken)
    {
        var response = await sqs.GetQueueAttributesAsync(
            new GetQueueAttributesRequest
            {
                QueueUrl = settings.QueueUrl,
                AttributeNames = ["ApproximateNumberOfMessages"]
            },
            cancellationToken);

        if (response.Attributes is null ||
            !response.Attributes.TryGetValue(
                "ApproximateNumberOfMessages",
                out var countText) ||
            !long.TryParse(
                countText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var count) ||
            count < 0)
        {
            throw new InvalidDataException(
                "SQS did not return a valid nonnegative DLQ message count.");
        }

        await cloudWatch.PutMetricDataAsync(
            new PutMetricDataRequest
            {
                Namespace = settings.Namespace,
                MetricData =
                [
                    new MetricDatum
                    {
                        MetricName = settings.MetricName,
                        Value = count,
                        Unit = StandardUnit.Count,
                        Timestamp = DateTime.UtcNow
                    }
                ]
            },
            cancellationToken);

        logger.LogInformation(
            "Published DLQ count {Count} to {Namespace}/{MetricName}.",
            count,
            settings.Namespace,
            settings.MetricName);
    }
}