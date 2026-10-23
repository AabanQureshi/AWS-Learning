# Learning AWS Services with .NET

A hands-on learning project for using AWS services from an ASP.NET Core application with the official AWS SDK for .NET.

The project targets **.NET 10** and uses straightforward code so the focus stays on configuring services, making SDK requests, and understanding their behavior.

## Current state

The repository currently contains a runnable ASP.NET Core MVC baseline with:

- Standard application configuration and logging.
- A liveness endpoint at `/health`.
- Official SDK packages for Amazon S3, Amazon SQS, and Amazon DynamoDB.

AWS client registration, service configuration, and document-processing features will be implemented as learning exercises. They are not included in the current baseline.

## Learning goals

| Service or concept | Hands-on topics |
| --- | --- |
| Amazon S3 | Buckets, object keys, prefixes, uploads, and downloads |
| Amazon SQS | Sending and receiving messages, visibility timeouts, acknowledgement, retries, and dead-letter queues |
| Amazon DynamoDB | Tables, partition keys, items, reads, and conditional updates |
| AWS IAM | Identities, credential profiles, permission policies, and least privilege |
| AWS SDK for .NET | Client configuration, dependency injection, asynchronous requests, and cancellation |
| Background processing | Queue consumers, job status, duplicate delivery, and recovery |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
- An IDE or editor with C# support.
- AWS CLI v2 for credential profiles and service operations when needed.
- An AWS account or a reachable AWS-compatible emulator for the service exercises.

No AWS connection is required to run the baseline application.

## Run the application

Open `AWS-Learning.slnx` in your IDE, or run these commands from the repository root:

```powershell
dotnet restore AWS-Learning.slnx
dotnet build AWS-Learning.slnx
dotnet run --project AWS-Learning --launch-profile https
```

If the local ASP.NET Core development certificate is not trusted, follow the .NET development certificate setup for your operating system. On Windows:

```powershell
dotnet dev-certs https --trust
```

With the included launch profile:

- Home page: `https://localhost:7020/`
- Application health: `https://localhost:7020/health`

The health endpoint returns `Healthy` when the application can respond. It does not check AWS credentials, network connectivity, or service resources.

## SDK packages

The project already references:

```text
AWSSDK.S3
AWSSDK.SQS
AWSSDK.DynamoDBv2
```

The exercises will use these clients directly rather than adding CQRS or additional architecture frameworks.

## Local emulation and real AWS

Local exercises use Floci as an AWS-compatible emulator. The SDK clients will need an explicitly configured emulator endpoint, signing region, and credentials. For local S3 access, path-style addressing will also be configured.

`localhost` refers to the machine or container running the application. An emulator on another machine requires a reachable private address or an existing forwarding connection.

For real AWS, use the appropriate AWS service endpoints and credentials, and validate permissions and behavior against the actual services. Emulator compatibility does not imply identical IAM enforcement, reliability, networking, or service coverage.

The initial lab's negative authorization checks succeeded where the intended policies did not grant access. Permission isolation was therefore not established in that emulator setup. Keep emulator endpoints private and do not use those results as proof of AWS authorization behavior.

## Planned practice application

A document-processing workflow will connect the services as the lessons progress:

```mermaid
flowchart LR
    Client[Client] --> Api[ASP.NET Core application]
    Api --> Storage[S3 document storage]
    Api --> Jobs[DynamoDB job status]
    Api --> Queue[SQS processing queue]
    Queue --> Worker[Background worker]
    Worker --> Storage
    Worker --> Jobs
    Queue --> DLQ[Dead-letter queue after repeated delivery failures]
```

The first processor will calculate text statistics and store a JSON result. This provides a small end-to-end exercise before introducing more expensive processing.

## Credentials and configuration

- Keep access keys and secret access keys out of source code, committed configuration, and documentation.
- Use local named credential profiles for development exercises.
- Use workload roles and temporary credentials where appropriate for deployment to AWS.
- Separate API and worker permissions according to their required operations.
- Store machine-specific overrides outside committed files, such as environment variables or .NET user secrets.

Changing Git branches does not reset emulator or AWS resources. Use identifiable exercise data and manage resource cleanup separately.

## References

- [AWS SDK for .NET documentation](https://docs.aws.amazon.com/sdk-for-net/)
- [Amazon S3 documentation](https://docs.aws.amazon.com/s3/)
- [Amazon SQS documentation](https://docs.aws.amazon.com/sqs/)
- [Amazon DynamoDB documentation](https://docs.aws.amazon.com/dynamodb/)
- [AWS IAM documentation](https://docs.aws.amazon.com/iam/)
