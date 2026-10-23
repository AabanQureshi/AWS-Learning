using AWS_Learning.Models;

namespace AWS_Learning.Services;

public interface IDocumentService
{
    Task<UploadDocumentResponse> UploadAsync(Stream content, string fileName, long size, CancellationToken cancellationToken);
    Task<JobStatusResponse?> GetJobAsync(string jobId, CancellationToken cancellationToken);
    Task<StoredDocument> GetResultAsync(string jobId, CancellationToken cancellationToken);
}
