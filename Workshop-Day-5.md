# AWS Day 5 workshop — Lambda + SQS

## Goal

Run a C# Lambda function directly, then process a separate SQS practice queue through an event-source mapping.

## Evidence completed

- Lambda `aaban-day5-document-summary` deployed with `dotnet8`.
- Execution role and scoped log permissions created.
- Direct invocation succeeded.
- SQS event-source mapping succeeded.
- Verified log: `Processed JobId=day5-job-004 ... WordCount=1, CharacterCount=30`.

## Code change

The handler changed from `FunctionHandler(string input, ...)` to `FunctionHandler(SQSEvent sqsEvent, ...)`. Each SQS record body is deserialized as JSON into `DocumentJob`. Failures are returned through `SQSBatchResponse.BatchItemFailures`.

## Useful checks

```powershell
dotnet test .\tests\AWSLearning.Day5.Lambda.Tests\AWSLearning.Day5.Lambda.Tests.csproj --configuration Release
```

Valid message body:

```json
{"JobId":"day5-job-004","DocumentKey":"processed/example-document.txt"}
```

Malformed `JobId=...` text is intentionally rejected and retried. It is not valid JSON.

## Architecture

Worker polling: application owns `ReceiveMessage` and `DeleteMessage`.

Lambda mapping: AWS/Floci owns polling and acknowledgement; the handler owns business processing and reports failed record IDs.

## Next exercise

Inspect API Gateway’s installed REST API controls, then expose a small HTTP Lambda endpoint without connecting it to the existing document queue.
