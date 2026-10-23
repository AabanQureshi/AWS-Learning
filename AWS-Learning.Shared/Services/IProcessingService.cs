using AWS_Learning.Models;

namespace AWS_Learning.Services;

public interface IProcessingService
{
    Task ProcessAsync(DocumentJob job, CancellationToken cancellationToken);
}
