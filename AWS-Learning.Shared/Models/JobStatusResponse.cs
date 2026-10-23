namespace AWS_Learning.Models;

/// <summary>
/// Reports the current processing status and optional result location of a job.
/// </summary>
public sealed record JobStatusResponse(
    string JobId,
    string Status,
    string? ResultKey,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
