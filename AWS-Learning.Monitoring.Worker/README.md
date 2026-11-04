# Monitoring Worker — Day 3

This standalone .NET 10 Worker Service is included in the solution on the `day-3` branch. It reads the document dead-letter queue's available-message count and publishes a custom CloudWatch metric.

The collector publishes `DocumentJobsDlqVisibleMessages` in the `Learning/Documents` namespace about once per minute. Successful reads publish zero when the queue is empty; failed reads log an error rather than publish a fabricated zero.

The monitor is a separate process so stopping the document Worker does not also stop metric collection. It uses the dedicated `floci-monitor` profile. Never put credentials in this project.

The AWS logging provider sends existing `ILogger` output to `/aws-learning/monitoring-worker` while preserving console logging. The logging client and metric clients share credentials and the configured monitoring endpoint. Deployment requires the additional log-stream permissions described in [the centralized logging guide](../docs/day-3-centralized-logging.md).
