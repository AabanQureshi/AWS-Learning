# Day 5: a C# Lambda function

## What is implemented

Created with AWS's `lambda.EmptyFunction` template from Amazon.Lambda.Templates 8.1.2. The template currently defaults to .NET 10; this isolated exercise targets .NET 8 to match the selected `dotnet8` runtime in Floci. Existing application hosts keep .NET 10. The template's defaults file is optional tooling metadata, not deployed infrastructure.

The handler takes a **JSON string**, not an object with a Text property, and returns word count, UTF-16 code-unit count and a preview of at most 100 UTF-16 units. Words are whitespace-delimited; punctuation remains part of a word. The preview never cuts a surrogate pair in half. Empty input is valid; null is rejected. Logs contain a count, not the document text.

There is no Program.cs in the chosen class-library model: Lambda's managed .NET runtime loads the DLL, deserializes input, calls the handler and serializes its response. The execution context supplies invocation metadata and logging. CPU-only processing is synchronous; there is no I/O to wrap in a fake async method.

Handler: `AWSLearning.Day5.Lambda::AWSLearning.Day5.Lambda.Function::FunctionHandler`

These are respectively the assembly name, namespace plus class, and public method. The assembly serializer attribute connects JSON to the handler's C# types. This service has no connection to the existing S3 buckets, tables, queues or document Worker.

## Local verification and packaging

Run from the repository root in PowerShell:

```powershell
dotnet test ./tests/AWSLearning.Day5.Lambda.Tests/AWSLearning.Day5.Lambda.Tests.csproj --configuration Release
./exercises/day-5/Package.ps1
```

Package output: `artifacts/day-5/function.zip`. Publishing targets Linux x64 and uses the managed runtime, without bundling .NET. ReadyToRun is disabled for this small exercise to avoid requiring cross-platform native precompilation. Each package uses a fresh directory to avoid stale DLLs. The ZIP contains the published files directly at its root, including the assembly, dependency description and runtime configuration.

## Learner-run CLI deployment to Floci

These steps create resources and invoke code; packaging and unit tests alone do not run them. Commands below are for the local Floci lab only. The logging policy uses its observed account `000000000000` and region `us-east-1`; substitute the actual account/region for real AWS. Do not remove the explicit local endpoint when running this exercise.

Use a first-time deployment; if any create reports an existing resource, stop and inspect it before changing it. Run each block only after the preceding one succeeds.

```powershell
$lab = @('--endpoint-url', 'http://localhost:4566', '--region', 'us-east-1', '--profile', 'floci-lab')

aws iam create-role --role-name aaban-day5-lambda-role --assume-role-policy-document file://exercises/day-5/lambda-trust-policy.json @lab

aws logs create-log-group --log-group-name /aws/lambda/aaban-day5-document-summary @lab

aws iam put-role-policy --role-name aaban-day5-lambda-role --policy-name Day5LambdaLogs --policy-document file://exercises/day-5/lambda-logs-policy.json @lab
```

The trust policy allows Lambda to assume the role. Its separate permission policy allows writes only to this function's log streams. The function does not require S3, DynamoDB or SQS permissions.

```powershell
$roleArn = aws iam get-role --role-name aaban-day5-lambda-role --query Role.Arn --output text @lab
if ($LASTEXITCODE -ne 0) { throw 'Could not read Lambda role.' }

aws lambda create-function --function-name aaban-day5-document-summary --runtime dotnet8 --architectures x86_64 --handler 'AWSLearning.Day5.Lambda::AWSLearning.Day5.Lambda.Function::FunctionHandler' --role $roleArn --timeout 30 --memory-size 512 --zip-file fileb://artifacts/day-5/function.zip @lab
```

Check creation before invoking:

```powershell
aws lambda get-function-configuration --function-name aaban-day5-document-summary --query '{State:State,Reason:StateReason,Runtime:Runtime,Handler:Handler}' @lab
```

Continue once State is Active. If Pending, repeat this read; if Failed, preserve StateReason and diagnose. A configuration record is not proof of successful execution. Floci needs its existing Docker runtime/network configuration to launch the Lambda container; initial execution may require an image pull. Do not change the VM or container configuration speculatively if invocation fails.

```powershell
aws lambda invoke --function-name aaban-day5-document-summary --cli-binary-format raw-in-base64-out --payload file://exercises/day-5/input.json artifacts/day-5/response.json @lab
Get-Content ./artifacts/day-5/response.json
```

Expected response (property order/spacing may vary):

```json
{"WordCount":3,"CharacterCount":16,"Preview":"Hello AWS Lambda"}
```

Inspect both CLI metadata and the response: an HTTP success with FunctionError is still a handler failure. No deployed-function verification is claimed by the local tests. Queue mappings, retry behavior and API Gateway remain later exercises.

## References

- [AWS C# handler models](https://docs.aws.amazon.com/lambda/latest/dg/csharp-handler.html)
- [AWS .NET templates and tools](https://docs.aws.amazon.com/lambda/latest/dg/csharp-package-cli.html)

## Verification evidence

| Requirement | Test |
|---|---|
| Whitespace word counts, empty input, UTF-16 count | SummarizesWordsAndUtf16Units |
| Preview before, at and after limit | LimitsPreviewToOneHundredUtf16Units |
| Unicode preview boundary | PreviewDoesNotSplitSurrogatePair |
| Null guards | RejectsNullInput; RejectsNullContext |
| Invocation logging | WritesInvocationLog |
| Actual Lambda JSON serializer contract | LambdaSerializerRoundTripsJsonStringAndSummary |

All 13 cases passed with `dotnet test tests/AWSLearning.Day5.Lambda.Tests/AWSLearning.Day5.Lambda.Tests.csproj --configuration Release`. Full solution Release build passed with zero warnings/errors. Linux-x64 ZIP inspected: DLL, dependencies and runtime configuration are at its root. Live deployment/invocation remains pending.
