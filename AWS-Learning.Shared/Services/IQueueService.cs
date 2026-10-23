using AWS_Learning.Models;

namespace AWS_Learning.Services;

public interface IQueueService
{
    Task SendAsync(DocumentJobMessage message, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReceivedJobMessage>> ReceiveAsync(int maximumMessages, CancellationToken cancellationToken);
    Task DeleteAsync(string receiptHandle, CancellationToken cancellationToken);
    Task ChangeVisibilityAsync(string receiptHandle, int visibilityTimeoutSeconds, CancellationToken cancellationToken);
}
