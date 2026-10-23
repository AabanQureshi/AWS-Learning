# Day 1: document flow

Branch: `day-1-documents`. The `main` branch remains the reviewed service/setup snapshot with a fresh web template and an idle Worker. Exercise changes stay on this branch.

## Run in your IDE

1. Open `AWS-Learning.slnx`.
2. Configure multiple startup projects: `AWS-Learning` and `AWS-Learning.Worker`, both set to Start.
3. Use the web project's `https` launch profile and the Worker's included Development profile.
4. Confirm your existing local Floci forwarding is running and your `floci-api` and `floci-worker` profiles exist.
5. Start debugging. Open `https://localhost:7020/documents`.

From two terminals instead:

```powershell
dotnet run --project AWS-Learning --launch-profile https
```

```powershell
dotnet run --project AWS-Learning.Worker
```

Do not run a second Worker while debugging the first one. This lesson is designed around one consumer; a database ownership lease is not implemented.

## Walk through each request

| Flow | Breakpoint locations | Expected behavior |
| --- | --- | --- |
| Upload | `DocumentController.Upload`, `DocumentService.UploadAsync` | Validate a nonempty .txt file up to 5 MB; create a unique ID |
| Store input | `StorageService.UploadAsync` | Put bytes at `incoming/{jobId}.txt` |
| Create job | `JobService.CreateAsync` | Create a Pending item with an atomic no-overwrite condition |
| Publish work | `QueueService.SendAsync` | Send camel-case JSON with the job ID and input location |
| Receive work | `QueueService.ReceiveAsync`, `Worker.ProcessDeliveryAsync` | Long-poll one message; load the persisted job and validate the payload's source |
| Process | `ProcessingService.ProcessAsync`, `StorageService.DownloadAsync` | Read text, count whitespace-separated words and UTF-16 code units |
| Save output | `StorageService.UploadAsync`, `JobService.UpdateStatusAsync` | Write `processed/{jobId}.json`, then mark Completed |
| Acknowledge | `QueueService.DeleteAsync` | Delete the current message delivery only after successful processing |
| Status | `DocumentController.Details`, `DocumentService.GetJobAsync` | Retrieve current DynamoDB state |
| Preview/download | `DocumentController.Result` or `Download`, `DocumentService.GetResultAsync` | Read the completed S3 result and dispose its stream after use |

The status page defaults to manual refresh, keeping background requests out of the way of breakpoints. Optional polling checks status every five seconds while enabled on that page.

## Useful experiments

- Stop the Worker, upload a document, and observe Pending. Start the Worker and refresh to observe Completed.
- Upload `Hello AWS learning lab` without a trailing newline: expect 4 words and 22 UTF-16 code units.
- Download the JSON and compare its job ID and counts with the result page.
- Look up a nonexistent ID: expect a 404 notice.
- Request a result before completion: expect a 409 notice.
- Submit an empty file or a non-.txt file: expect validation feedback and no successful upload response.
- View an existing failed job: the page explains that an attempt failed, not that the queue message has necessarily reached the DLQ.

The manually inserted introductory item may lack `CreatedAt`. The application requires its complete job schema; use jobs created through the app for this exercise. Malformed stored data is reported separately from a missing job.

## Worker behavior

The Worker creates a dependency injection scope for each receive/processing unit. Successful processing is followed by acknowledgement. Failure leaves the message unacknowledged and records Failed when possible. The queue's redrive policy decides when repeated deliveries go to the DLQ.

While processing, the Worker requests a 60-second visibility period every 20 seconds. A debugger can pause all threads, including that heartbeat; do not assume visibility stays renewed while execution is suspended. Completed jobs are skipped on redelivery so an acknowledgement retry does not repeat successful processing.

There is no direct transfer of control from the controller to the Worker. They are independent processes connected through SQS and the stored job ID.

## Limits of this lesson

- S3 upload, DynamoDB creation, and SQS publication are separate operations. No outbox or reconciliation is implemented.
- A status check is not a distributed lock. Multiple Worker processes can duplicate computation; run one for this lesson.
- File extension validation does not establish that arbitrary uploaded bytes are valid UTF-8 text.
- Emulator authorization did not reject the initial negative permission checks. Do not expose the emulator publicly or treat its behavior as proof of least privilege.
- Schema names follow `nameof(DocumentJob.Property)`. Renaming a persisted property needs a data migration or explicit mapping.

Git branches isolate application code, not AWS/Floci state. Exercise objects, jobs, and messages remain until you deliberately manage them.
