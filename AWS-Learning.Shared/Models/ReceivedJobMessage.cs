namespace AWS_Learning.Models;

/// <summary>
/// Contains a queue delivery and the receipt handle needed to acknowledge it.
/// </summary>
public sealed record ReceivedJobMessage(
    string MessageId,
    string Body,
    string ReceiptHandle,
    int ReceiveCount);
