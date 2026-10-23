using AWS_Learning.Models;

namespace AWS_Learning.Services;

public interface IJobService
{
    Task CreateAsync(DocumentJob job, CancellationToken cancellationToken);
    Task<DocumentJob?> GetAsync(string jobId, CancellationToken cancellationToken);
    Task UpdateStatusAsync(string jobId, string status, string? resultKey, CancellationToken cancellationToken);
}
