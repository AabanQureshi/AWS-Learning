using AWS_Learning.Models;

namespace AWS_Learning.Services;

public interface IStorageService
{
    Task UploadAsync(Stream content, string key, string contentType, CancellationToken cancellationToken);
    Task<StoredDocument> DownloadAsync(string key, CancellationToken cancellationToken);
}
