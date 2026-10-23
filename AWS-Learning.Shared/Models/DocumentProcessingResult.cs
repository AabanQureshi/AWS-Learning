namespace AWS_Learning.Models;

/// <summary>
/// Contains the text statistics produced by a completed processing job.
/// </summary>
public sealed record DocumentProcessingResult(
    string JobId,
    int WordCount,
    int CharacterCount,
    DateTimeOffset ProcessedAt);
