# Guided debugging: Day 1

The teaching breakpoints are installed in the open Visual Studio solution. There are 26 locations. Upload is the initial selected phase, with six enabled stops. All enables 24 locations, leaving the two optional configuration locations disabled. No application code was changed to force a debugger stop.

## Before you start

1. Save your source files. The breakpoint helper resolves locations from saved method/statement anchors, not fixed line numbers.
2. Open Debug > Windows > Breakpoints to inspect the installed breakpoints.
3. Keep automatic status refresh off on the job page.
4. Run only one Worker process. Stop any copies started from a terminal before starting it in Visual Studio.
5. Use F10 to step over a statement, F11 to enter your own method, Shift+F11 to step out, and F5 to continue to the next breakpoint. Avoid stepping into AWS SDK/framework internals for this first exercise.
6. Inspect Locals and Call Stack. When both projects are debugged, notice which process is active: the browser request and Worker are independent executions.

Do not read a stream in Watch or Immediate just to inspect it: doing so can advance its position. Inspect `CanRead`, metadata, keys, and already-created text/result variables instead. Do not copy credential secrets into logs or this repository.

## Select one phase

Run these from the repository root while this solution is open in Visual Studio. Prefer switching phases before starting or restarting debugging.

```powershell
./scripts/Set-LabBreakpoints.ps1 -Phase Upload
./scripts/Set-LabBreakpoints.ps1 -Phase Worker
./scripts/Set-LabBreakpoints.ps1 -Phase Read
./scripts/Set-LabBreakpoints.ps1 -Phase Failures
./scripts/Set-LabBreakpoints.ps1 -Phase Duplicate
```

Each invocation enables that phase's teaching locations and disables the other teaching locations. It does not delete unrelated breakpoints. Duplicate also enables the acknowledgement point W8.

Other options:

```powershell
# All normal flow locations; configuration stays disabled.
./scripts/Set-LabBreakpoints.ps1 -Phase All

# Only registration/profile-selection locations.
./scripts/Set-LabBreakpoints.ps1 -Phase Configuration

# Disable the teaching locations without deleting them.
./scripts/Set-LabBreakpoints.ps1 -Phase None

# Show current source locations and suggested inspection expressions without changing Visual Studio.
./scripts/Set-LabBreakpoints.ps1 -ListOnly | Format-Table Id, Phase, Line, Inspect -Wrap
```

The IDs below identify the plan entries; they are not Visual Studio breakpoint labels. The portable definitions are in `debugging/day-1-breakpoints.json`. The helper reuses existing breakpoints at matching locations, rather than adding duplicates each time it runs.

## Exercise 1: upload and enqueue, with the Worker stopped

Select the Upload phase. Configure the web project to Start and the Worker to None in the solution's multiple startup projects settings. Start debugging and open `https://localhost:7020/documents`.

Create a small file with no trailing newline:

```powershell
[IO.File]::WriteAllText((Join-Path $env:TEMP 'aws-debug.txt'), 'Hello AWS learning lab')
```

Choose that file and click Upload & queue.

| Stop | Inspect | What to establish |
| --- | --- | --- |
| U1: controller validation | `model.File.FileName`, `model.File.Length`, `ModelState.IsValid` | An HTTP upload arrives as IFormFile |
| U2: document service ID creation | `fileName`, `size`, `content.CanRead`; step over for `jobId` | The service receives a stream and generates its own storage identity |
| U3: S3 PutObject | `request.BucketName`, `request.Key`, `request.ContentType` | The key is `incoming/{jobId}.txt`; F10 performs the actual network operation |
| U4: DynamoDB PutItem | `request.Item`, `request.ConditionExpression`, `request.ExpressionAttributeNames` | The item is Pending; the condition prevents overwriting a job |
| U5: SQS SendMessage | `request.MessageBody`, `request.QueueUrl` | The queue receives a reference, not the file bytes |
| U6: upload response | `jobId`, `sourceKey`, `size` | Storage, job creation, and sending have returned successfully |

Continue until the browser displays Pending. Copy the job ID for the following exercises. While the Worker is stopped, inspect the input in Floci S3, its item in DynamoDB, and its message in the main queue.

Expected insight: a successful upload does not mean processing has finished.

## Exercise 2: receive, process, save, acknowledge

Stop debugging, select the Worker phase, then configure both projects to Start. Start debugging. The queued job from Exercise 1 should trigger the Worker without another upload.

| Stop | Inspect | What to establish |
| --- | --- | --- |
| W1: receive response | `response.Messages` | The condition stops only when at least one message exists; empty polling does not interrupt you |
| W2: Worker job lookup | `payload`, `message.MessageId`, `message.ReceiveCount`, `jobId` | The Worker deserializes the instruction and retrieves the stored job |
| W3: completed-job check | `currentJob.Status`, `currentJob.SourceKey` | A new job proceeds; a completed one is skipped |
| W4: DynamoDB update | `status`, `resultKey`, `request.UpdateExpression` | First observe Processing; the same point is hit again for Completed |
| W5: S3 download response | `key`, `response.Headers.ContentType`, `response.ResponseStream.CanRead` | The download returns a readable stream |
| W6: result serialization | `text`, `result.WordCount`, `result.CharacterCount` | Expect 4 words and 22 UTF-16 code units for the exact sample |
| W7: result upload | `resultKey`, `resultBytes.Length` | The processed output is JSON stored at `processed/{jobId}.json` |
| W8: acknowledgement | `receiptHandle`, Call Stack | The Worker acknowledges only after processing and the Completed update return successfully |

StorageService.UploadAsync is shared by source and result writes. The Upload phase's U3 point can be enabled too if you want to step inside the Worker's result upload.

After continuing, refresh the browser. It should show Completed. The original stays in S3, the result is present, and the successfully handled message is removed from the main queue.

### Pausing does not pause SQS

The emulator continues running while Visual Studio has your app paused. The Worker heartbeat also cannot run if the debugger suspends its process. Long pauses can let visibility expire and cause redelivery or an acknowledgement error. That is a real consequence of stopping a distributed consumer, not evidence that SQS is paused too. Keep pauses short for the first pass, use one Worker, and allow it to resume/recover if a completed job is redelivered. Do not rely on the heartbeat as a database lock.

## Exercise 3: lookup, preview, download

Select Read. Start the web app; the Worker is not needed to read a completed result.

1. Paste the saved ID into Find a job. At R1 inspect the route's `jobId`.
2. At R2 inspect `request.Key` and `response.Item`. This is a primary-key read, not a table scan.
3. At R3 inspect how DocumentJob is mapped into JobStatusResponse.
4. Click View result. At R4 inspect the Completed requirement and `job.ResultKey`.
5. At R5 inspect the downloaded `json`; step over to see the typed `statistics`.
6. Click Download JSON. At R6 inspect filename, content type, and readable stream. Continue and let FileStreamResult send/dispose it.

Expected insight: the web app reads a previously computed result; clicking View result does not process the file again.

## Exercise 4: validation and unavailable data

Select Failures.

- Upload a `.csv` file. F1 catches the service's ArgumentException and converts it into validation feedback (400). No successful document response is returned.
- Find `this-job-does-not-exist`. F2 shows KeyNotFoundException becoming a 404 notice.
- With the Worker stopped, upload another valid file and open `/documents/{new-job-id}/result` before processing. F2 shows InvalidOperationException becoming 409.
- An empty file also reaches the service validation path. Submitting no file is rejected by browser/model validation and may not reach F1.
- F3 is enabled for an actual processing/acknowledgement error. Inspect `processingCompleted`: a successful job must not be downgraded merely because acknowledgement fails. This exercise does not deliberately break existing resources to force that error.

The old manually inserted introductory item can lack CreatedAt and produce 502. Use new app-created jobs for the normal flow.

## Exercise 5: duplicate completed work

Select Duplicate and run the Worker. In Floci, open the main SQS queue, Messages > Send message. Send the completed job's original reference again:

```json
{
  "jobId": "YOUR_COMPLETED_JOB_ID",
  "bucket": "aaban-day1-documents",
  "key": "incoming/YOUR_COMPLETED_JOB_ID.txt"
}
```

Replace both occurrences with the ID created by this app. Leave delivery delay at zero.

At D1, `currentJob.Status` is Completed. The processor returns without another document read/count/result write. Continue to W8 to observe acknowledgement of this new delivery. The new SQS message ID can differ even though the application JobId is the same.

This demonstrates completed-job duplicate handling; it does not prove that two concurrent workers cannot perform the same unfinished job.

## Optional: configuration and credentials

Select Configuration before starting the hosts. C1 shows whether the host selected the API or Worker profile. C2 shows the selected profile name. SDK clients are constructed lazily when resolved, so credential lookup may happen on a request or Worker receive rather than at registration time. There can be one stop per resolved client.

Return to All or a specific flow phase afterward.
