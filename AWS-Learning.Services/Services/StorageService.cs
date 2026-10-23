using Amazon.S3;
using Amazon.S3.Model;
using AWS_Learning.Configuration;
using AWS_Learning.Models;
using AWS_Learning.Services;
using Microsoft.Extensions.Options;

namespace AWS_Learning.Services;

public sealed class StorageService(
    IAmazonS3 s3,
    IOptions<AWSConfig> options) : IStorageService
{
    public async Task UploadAsync(
        Stream content,
        string key,
        string contentType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var request = new PutObjectRequest
        {
            BucketName = options.Value.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };

        await s3.PutObjectAsync(request, cancellationToken);
    }

    public async Task<StoredDocument> DownloadAsync(
        string key,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var request = new GetObjectRequest
        {
            BucketName = options.Value.BucketName,
            Key = key
        };

        var response = await s3.GetObjectAsync(
            request,
            cancellationToken);

        return new StoredDocument(
            response.ResponseStream,
            response.Headers.ContentType ?? "application/octet-stream",
            Path.GetFileName(key));
    }
}
