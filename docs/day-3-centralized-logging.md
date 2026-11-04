# Day 3: centralized application logging

All three hosts use `AWS.Logger.AspNetCore` 4.0.2 to add CloudWatch Logs as a destination for ordinary `ILogger` calls. Console logging remains enabled for Supervisor's local files. The provider batches delivery at five-second intervals and does not create log groups or change retention.

| Host | Profile | Log group | Stream |
| --- | --- | --- | --- |
| Web | `floci-api` | `/aws-learning/web` | `i-10e9e7cb100b85103` |
| Document Worker | `floci-worker` | `/aws-learning/document-worker` | `i-0a87a838d83a46a57` |
| Monitoring Worker | `floci-monitor` | `/aws-learning/monitoring-worker` | `i-0a87a838d83a46a57` |

These stream names identify this emulator exercise's instances. Change `CloudWatchLogs:LogStreamName` when deploying another instance. Credentials remain in local profile files, never in the repository.

## Configuration decisions

- Each application writes with its own identity. Logging does not require an administrator profile.
- Web logging reuses `Aws:ServiceUrl`, `Aws:Region` and `Aws:ApiProfile`. Monitoring logging reuses the corresponding `Monitoring` settings and shares its resolved credentials with the SQS and CloudWatch metric clients.
- Explicit profile resolution rejects a missing profile instead of silently switching identities. Credentials are now checked during web and monitoring host setup, before the host starts.
- Existing instance endpoint overrides continue to apply: `Aws__ServiceUrl` for web and `Monitoring__ServiceUrl` for monitoring. Running on the PC uses `http://localhost:4566`; the instances use `http://floci:4566`.
- Log groups are provisioned separately with seven-day retention. The provider still attempts stream creation, so each identity needs `logs:CreateLogStream` and `logs:PutLogEvents`, scoped to its own stream.
- Delivery is asynchronous. A successful build is not proof of delivery, and buffered messages can be lost on an abrupt process or instance failure. Supervisor's local log rotation and CloudWatch retention are separate settings.

## Apply the additional permissions

Run in Windows PowerShell from the solution directory. These commands add separate inline policies and do not replace the identities' existing policies. The document Worker's logging policy was configured in the earlier exercise.

```powershell
$webPolicyPath = (Resolve-Path '.\infrastructure\iam\day3-web-logging-policy.json').Path
$monitorPolicyPath = (Resolve-Path '.\infrastructure\iam\day3-monitor-logging-policy.json').Path

aws --endpoint-url http://localhost:4566 --region us-east-1 --profile floci-lab iam put-user-policy `
  --user-name aaban-document-api `
  --policy-name Day3WebLogs `
  --policy-document "file://$webPolicyPath"

if ($LASTEXITCODE -ne 0) { throw 'Web logging policy assignment failed.' }

aws --endpoint-url http://localhost:4566 --region us-east-1 --profile floci-lab iam put-user-policy `
  --user-name aaban-day3-monitor `
  --policy-name Day3MonitorLogs `
  --policy-document "file://$monitorPolicyPath"

if ($LASTEXITCODE -ne 0) { throw 'Monitor logging policy assignment failed.' }
```

## Deployment and verification

1. Publish the updated web and monitoring projects for `linux-x64`, framework-dependent. Do not start a second document Worker on the PC.
2. Stop only the host being updated through Supervisor, transfer its publish output, then start it again. Keep credentials outside deployment directories. Web remains in `/opt/day2-web`; monitoring remains in `/opt/day3-monitor`.
3. Preserve the existing Supervisor environment and credentials-file overrides. No credential transfer is required for these logging changes.
4. Verify `aws-web` on the web instance and `aws-monitor` on the private instance are running. Leave `aws-worker` running.
5. Open the corresponding CloudWatch Logs group and stream. Clear any earlier manual-test filter and allow time for the provider's batch delivery.
6. Verify fresh web startup events and fresh monitor `Published DLQ count` events. Validate a document upload and result separately: startup logs do not establish business-workflow health.

The code changes do not apply IAM policies, redeploy applications or establish runtime delivery by themselves. The monitoring alarm's SNS action is configured, but automatic SNS delivery is absent from the published Floci 2.3.0 implementation; see the learning log for the version-specific investigation.
