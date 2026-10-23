namespace AWS_Learning.Worker;

public sealed class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker host is ready. Queue polling and processing are not implemented yet.");
        return Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
