using Amazon.SQS;
using Amazon.SQS.Model;
using AWS_Learning.Configuration;
using AWS_Learning.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AWS_Learning.Services;

public sealed class QueueService : IQueueService
{
    private readonly AWSConfig options;
    private readonly IAmazonSQS sqs;

    public QueueService(IOptions<AWSConfig> _options, IAmazonSQS sQS)
    {
        options = _options.Value;
        sqs = sQS;
    }
    public async Task SendAsync(
    DocumentJobMessage message,
    CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.JobId);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Bucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Key);

        var body = JsonSerializer.Serialize(
            message,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var request = new SendMessageRequest
        {
            QueueUrl = options.QueueUrl,
            MessageBody = body
        };

        await sqs.SendMessageAsync(request, cancellationToken);
    }
    public async Task<IReadOnlyList<ReceivedJobMessage>> ReceiveAsync(
    int maximumMessages,
    CancellationToken cancellationToken)
    {
        if (maximumMessages is < 1 or > 10)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumMessages),
                "Receive between 1 and 10 messages.");
        }

        var request = new ReceiveMessageRequest
        {
            QueueUrl = options.QueueUrl,
            MaxNumberOfMessages = maximumMessages,
            WaitTimeSeconds = 20,
            MessageSystemAttributeNames = ["ApproximateReceiveCount"]
        };

        var response = await sqs.ReceiveMessageAsync(
            request,
            cancellationToken);

        var receivedMessages = new List<ReceivedJobMessage>();

        if (response.Messages is null)
        {
            return receivedMessages;
        }

        foreach (var message in response.Messages)
        {
            var receiveCount = 0;

            if (message.Attributes is not null &&
                message.Attributes.TryGetValue(
                    "ApproximateReceiveCount",
                    out var countText))
            {
                int.TryParse(countText, out receiveCount);
            }

            receivedMessages.Add(new ReceivedJobMessage(
                message.MessageId,
                message.Body,
                message.ReceiptHandle,
                receiveCount));
        }

        return receivedMessages;
    }
    public async Task DeleteAsync(
    string receiptHandle,
    CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(receiptHandle);

        var request = new DeleteMessageRequest
        {
            QueueUrl = options.QueueUrl,
            ReceiptHandle = receiptHandle
        };

        await sqs.DeleteMessageAsync(request, cancellationToken);
    }
    public async Task ChangeVisibilityAsync(
    string receiptHandle,
    int visibilityTimeoutSeconds,
    CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(receiptHandle);

        if (visibilityTimeoutSeconds is < 0 or > 43200)
        {
            throw new ArgumentOutOfRangeException(
                nameof(visibilityTimeoutSeconds),
                "Visibility must be between 0 and 43200 seconds.");
        }

        var request = new ChangeMessageVisibilityRequest
        {
            QueueUrl = options.QueueUrl,
            ReceiptHandle = receiptHandle,
            VisibilityTimeout = visibilityTimeoutSeconds
        };

        await sqs.ChangeMessageVisibilityAsync(
            request,
            cancellationToken);
    }
}
