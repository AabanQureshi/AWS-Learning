using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AWS_Learning.Configuration;
using AWS_Learning.Models;
using AWS_Learning.Services;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace AWS_Learning.Services;

public sealed class JobService : IJobService
{
    private readonly AWSConfig options;
    private readonly IAmazonDynamoDB dynamoDb;

    public JobService(IOptions<AWSConfig> _options, IAmazonDynamoDB dynamoDB )
    {
        options = _options.Value;
        dynamoDb = dynamoDB;
    }
    public async Task CreateAsync(
    DocumentJob job,
    CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentException.ThrowIfNullOrWhiteSpace(job.JobId);

        var item = new Dictionary<string, AttributeValue>
        {
            [nameof(DocumentJob.JobId)] = new() { S = job.JobId },
            [nameof(DocumentJob.Status)] = new() { S = job.Status },
            [nameof(DocumentJob.Bucket)] = new() { S = job.Bucket },
            [nameof(DocumentJob.SourceKey)] = new() { S = job.SourceKey },
            [nameof(DocumentJob.CreatedAt)] = new() { S = job.CreatedAt.ToString("O") }
        };

        if (job.ResultKey is not null)
        {
            item[nameof(DocumentJob.ResultKey)] = new AttributeValue
            {
                S = job.ResultKey
            };
        }

        if (job.CompletedAt is not null)
        {
            item[nameof(DocumentJob.CompletedAt)] = new AttributeValue
            {
                S = job.CompletedAt.Value.ToString("O")
            };
        }

        var request = new PutItemRequest
        {
            TableName = options.JobsTableName,
            Item = item,
            ConditionExpression = "attribute_not_exists(#jobId)",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#jobId"] = nameof(DocumentJob.JobId)
            }
        };

        await dynamoDb.PutItemAsync(request, cancellationToken);
    }
    public async Task<DocumentJob?> GetAsync(
    string jobId,
    CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        var request = new GetItemRequest
        {
            TableName = options.JobsTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [nameof(DocumentJob.JobId)] = new() { S = jobId }
            },
            ConsistentRead = true
        };

        var response = await dynamoDb.GetItemAsync(
            request,
            cancellationToken);

        if (response.Item is null || response.Item.Count == 0)
        {
            return null;
        }

        var item = response.Item;

        string[] requiredAttributes =
        [
            nameof(DocumentJob.JobId), nameof(DocumentJob.Status), nameof(DocumentJob.Bucket),
            nameof(DocumentJob.SourceKey), nameof(DocumentJob.CreatedAt)
        ];
        if (requiredAttributes.Any(name =>
                !item.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value.S)))
        {
            throw new InvalidDataException("The stored job is missing required string attributes.");
        }

        var resultKey = item.TryGetValue(nameof(DocumentJob.ResultKey), out var result)
            ? result.S
            : null;

        DateTimeOffset? completedAt =
            item.TryGetValue(nameof(DocumentJob.CompletedAt), out var completed)
                ? DateTimeOffset.ParseExact(
                    completed.S,
                    "O",
                    CultureInfo.InvariantCulture)
                : null;

        return new DocumentJob(
            item[nameof(DocumentJob.JobId)].S,
            item[nameof(DocumentJob.Status)].S,
            item[nameof(DocumentJob.Bucket)].S,
            item[nameof(DocumentJob.SourceKey)].S,
            resultKey,
            DateTimeOffset.ParseExact(
                item[nameof(DocumentJob.CreatedAt)].S,
                "O",
                CultureInfo.InvariantCulture),
            completedAt);
    }
    public async Task UpdateStatusAsync(
    string jobId,
    string status,
    string? resultKey,
    CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        if (status is not ("Pending" or "Processing" or "Completed" or "Failed"))
        {
            throw new ArgumentException(
                "Unsupported job status.",
                nameof(status));
        }

        if (status == "Completed")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resultKey);
        }

        var values = new Dictionary<string, AttributeValue>
        {
            [":status"] = new() { S = status },
            [":completed"] = new() { S = "Completed" }
        };

        var updateExpression = "SET #status = :status";
        var names = new Dictionary<string, string>
        {
            ["#jobId"] = nameof(DocumentJob.JobId),
            ["#status"] = nameof(DocumentJob.Status)
        };

        if (resultKey is not null)
        {
            updateExpression += ", #resultKey = :resultKey";
            names["#resultKey"] = nameof(DocumentJob.ResultKey);
            values[":resultKey"] = new AttributeValue { S = resultKey };
        }

        if (status == "Completed")
        {
            updateExpression += ", #completedAt = :completedAt";
            names["#completedAt"] = nameof(DocumentJob.CompletedAt);
            values[":completedAt"] = new AttributeValue
            {
                S = DateTimeOffset.UtcNow.ToString("O")
            };
        }

        var request = new UpdateItemRequest
        {
            TableName = options.JobsTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [nameof(DocumentJob.JobId)] = new() { S = jobId }
            },
            UpdateExpression = updateExpression,
            ExpressionAttributeNames = names,
            ExpressionAttributeValues = values,
            ConditionExpression =
                "attribute_exists(#jobId) AND " +
                "(#status <> :completed OR :status = :completed)"
        };

        await dynamoDb.UpdateItemAsync(request, cancellationToken);
    }
}
