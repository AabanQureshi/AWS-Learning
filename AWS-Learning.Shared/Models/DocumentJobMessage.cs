namespace AWS_Learning.Models;

/// <summary>
/// Identifies the document to process and its application job.
/// </summary>
public sealed record DocumentJobMessage(string JobId, string Bucket, string Key);
