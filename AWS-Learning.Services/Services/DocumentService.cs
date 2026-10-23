using AWS_Learning.Configuration;
using AWS_Learning.Models;
using Microsoft.Extensions.Options;
using System.Collections;

namespace AWS_Learning.Services;

public sealed class DocumentService : IDocumentService
{
    private readonly AWSConfig options;
    private readonly IStorageService storage;
    private readonly IJobService jobs;
    private readonly IQueueService queue;

    public DocumentService(IOptions<AWSConfig> options, IStorageService storage, IJobService jobs, IQueueService queue)
    {
        this.options = options.Value;
        this.storage = storage;
        this.jobs = jobs;
        this.queue = queue;
    }
    public async Task<UploadDocumentResponse> UploadAsync(
    Stream content,
    string fileName,
    long size,
    CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (!content.CanRead)
        {
            throw new ArgumentException(
                "The content stream must be readable.",
                nameof(content));
        }

        if (size is <= 0 or > 5 * 1024 * 1024)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "The file must contain between 1 byte and 5 MB.");
        }

        if (!string.Equals(
                Path.GetExtension(fileName),
                ".txt",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Only .txt files are supported.",
                nameof(fileName));
        }

        var jobId = Guid.NewGuid().ToString("N");
        var bucket = options.BucketName;
        var sourceKey = $"incoming/{jobId}.txt";

        await storage.UploadAsync(
            content,
            sourceKey,
            "text/plain",
            cancellationToken);

        var job = new DocumentJob(
            jobId,
            "Pending",
            bucket,
            sourceKey,
            null,
            DateTimeOffset.UtcNow,
            null);

        await jobs.CreateAsync(job, cancellationToken);

        await queue.SendAsync(
            new DocumentJobMessage(jobId, bucket, sourceKey),
            cancellationToken);

        return new UploadDocumentResponse(
            jobId,
            bucket,
            sourceKey,
            size);
    }

    public async Task<JobStatusResponse?> GetJobAsync(
    string jobId,
    CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        var job = await jobs.GetAsync(jobId, cancellationToken);

        if (job is null)
        {
            return null;
        }

        return new JobStatusResponse(
            job.JobId,
            job.Status,
            job.ResultKey,
            job.CreatedAt,
            job.CompletedAt);
    }

    public async Task<StoredDocument> GetResultAsync(
    string jobId,
    CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        var job = await jobs.GetAsync(jobId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Job '{jobId}' was not found.");

        if (job.Status != "Completed" ||
            string.IsNullOrWhiteSpace(job.ResultKey))
        {
            throw new InvalidOperationException(
                "The job does not have a completed result.");
        }

        return await storage.DownloadAsync(
            job.ResultKey,
            cancellationToken);
    }
}
