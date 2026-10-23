namespace AWS_Learning.Models;

/// <summary>
/// Provides downloaded content. Its caller must dispose the content stream.
/// </summary>
public sealed record StoredDocument(Stream Content, string ContentType, string FileName);
