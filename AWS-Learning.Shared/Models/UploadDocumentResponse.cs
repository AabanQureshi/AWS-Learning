namespace AWS_Learning.Models
{
    /// <summary>
    /// Identifies a document successfully uploaded to storage.
    /// </summary>
    public sealed record UploadDocumentResponse(
        string DocumentId,
        string Bucket,
        string Key,
        long Size);
}
