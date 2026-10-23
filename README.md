# AWS Learning

A simple ASP.NET Core MVC application targeting .NET 10. Aaban implements AWS configuration and service interactions hands-on in daily exercise branches.

## Baseline on main

- Existing MVC home, privacy, and error pages.
- Standard ASP.NET Core configuration and logging.
- Application liveness endpoint: `/health`.
- Existing official SDK packages: `AWSSDK.S3`, `AWSSDK.SQS`, and `AWSSDK.DynamoDBv2`.
- No AWS credentials, client registrations, service endpoints, processing workers, or document operations.

The health endpoint reports whether the application can respond. It does not contact AWS or Floci, validate profiles, or verify resources.

## Run

From the solution folder:

```powershell
dotnet build AWS-Learning.slnx
dotnet run --project AWS-Learning --launch-profile https
```

Open `https://localhost:7020/` and `https://localhost:7020/health`.
The health response is `Healthy` when the application is running.

## Daily branches

Commit or stash any current exercise changes before switching branches.
Each new day starts from the clean baseline, rather than the previous day's exercise:

```powershell
git switch main
git switch -c Day-1
```

For the next day:

```powershell
git switch main
git switch -c Day-2
```

Keep exercise work on its daily branch. Do not merge it into `main` unless intentionally changing the shared baseline.

Git branches separate code, not Floci resources. Buckets, objects, queues, messages, and database items continue to exist when switching branches.

## Hands-on exercises

AWS endpoint settings, named profiles, dependency injection, SDK calls, job persistence, queue polling, retries, and processing are implemented during lessons.

The local emulator did not reject the negative permission checks in the initial lab. Its configured IAM policies are not proof that access isolation is enforced. Keep Floci private and validate actual authorization separately before relying on it.
