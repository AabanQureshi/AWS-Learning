namespace AWS_Learning.Models;

/// <summary>
/// Represents the persisted state of a document-processing job.
/// </summary>
public sealed record DocumentJob(
    string JobId,
    string Status,
    string Bucket,
    string SourceKey,
    string? ResultKey,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
