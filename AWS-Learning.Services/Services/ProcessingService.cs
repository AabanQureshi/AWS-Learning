using Amazon.DynamoDBv2;
using Amazon.S3;
using Amazon.SQS;
using AWS_Learning.Configuration;
using AWS_Learning.Models;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace AWS_Learning.Services;

public sealed class ProcessingService : IProcessingService
{
    private readonly AWSConfig options;
    private readonly IStorageService storage;
    private readonly IJobService jobs;

    public ProcessingService(IOptions<AWSConfig> _options, IStorageService storage, IJobService jobs)
    {
        options = _options.Value;
        this.storage = storage;
        this.jobs = jobs;
    }
    public async Task ProcessAsync(
    DocumentJob job,
    CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        var currentJob = await jobs.GetAsync(
            job.JobId,
            cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Job '{job.JobId}' was not found.");

        if (currentJob.Status == "Completed")
        {
            return;
        }

        if (!string.Equals(
                currentJob.Bucket,
                options.BucketName,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The job references an unexpected bucket.");
        }

        await jobs.UpdateStatusAsync(
            currentJob.JobId,
            "Processing",
            null,
            cancellationToken);

        var document = await storage.DownloadAsync(
            currentJob.SourceKey,
            cancellationToken);

        await using var input = document.Content;

        using var reader = new StreamReader(
            input,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 4096,
            leaveOpen: true);

        var text = await reader.ReadToEndAsync(cancellationToken);

        var result = new DocumentProcessingResult(
            currentJob.JobId,
            text.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries).Length,
            text.Length,
            DateTimeOffset.UtcNow);

        var resultBytes = JsonSerializer.SerializeToUtf8Bytes(
            result,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        await using var output = new MemoryStream(resultBytes);

        var resultKey = $"processed/{currentJob.JobId}.json";

        await storage.UploadAsync(
            output,
            resultKey,
            "application/json",
            cancellationToken);

        await jobs.UpdateStatusAsync(
            currentJob.JobId,
            "Completed",
            resultKey,
            cancellationToken);
    }
}
