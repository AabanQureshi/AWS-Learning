using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

[assembly: LambdaSerializer(
    typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLearning.Day5.Lambda;

public sealed record DocumentJob(
    string JobId,
    string DocumentKey);

public sealed record JobResult(
    string JobId,
    string DocumentKey,
    int WordCount,
    int CharacterCount);

public sealed class Function
{
    public SQSBatchResponse FunctionHandler(
        SQSEvent sqsEvent,
        ILambdaContext context)
    {
        ArgumentNullException.ThrowIfNull(sqsEvent);
        ArgumentNullException.ThrowIfNull(context);

        var failures = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var message in sqsEvent.Records)
        {
            try
            {
                ProcessMessage(message, context);
            }
            catch (Exception exception)
            {
                context.Logger.LogLine(
                    $"Failed message {message.MessageId}: {exception.Message}");

                failures.Add(
                    new SQSBatchResponse.BatchItemFailure
                    {
                        ItemIdentifier = message.MessageId
                    });
            }
        }

        return new SQSBatchResponse
        {
            BatchItemFailures = failures
        };
    }

    private static void ProcessMessage(
        SQSEvent.SQSMessage message,
        ILambdaContext context)
    {
        if (string.IsNullOrWhiteSpace(message.Body))
        {
            throw new ArgumentException("SQS message body is empty.");
        }

        var job = System.Text.Json.JsonSerializer.Deserialize<DocumentJob>(
            message.Body,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })
            ?? throw new ArgumentException("Message body is not valid JSON.");

        if (string.IsNullOrWhiteSpace(job.JobId))
        {
            throw new ArgumentException("JobId is required.");
        }

        if (string.IsNullOrWhiteSpace(job.DocumentKey))
        {
            throw new ArgumentException("DocumentKey is required.");
        }

        int wordCount = job.DocumentKey
            .Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries)
            .Length;

        int characterCount = job.DocumentKey.Length;

        var result = new JobResult(
            job.JobId,
            job.DocumentKey,
            wordCount,
            characterCount);

        context.Logger.LogLine(
            $"Processed JobId={result.JobId}, " +
            $"DocumentKey={result.DocumentKey}, " +
            $"WordCount={result.WordCount}, " +
            $"CharacterCount={result.CharacterCount}");
    }
}