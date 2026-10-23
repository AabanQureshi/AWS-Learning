namespace AWS_Learning.Models;

public sealed record DocumentResultViewModel(
    string JobId,
    DocumentProcessingResult Statistics,
    string Json);
