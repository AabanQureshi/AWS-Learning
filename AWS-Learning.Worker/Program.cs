using Amazon.Runtime.CredentialManagement;
using AWS.Logger;
using AWS_Learning;
using AWS_Learning.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

var profileName = RequiredSetting("Aws:WorkerProfile");
var profiles = new CredentialProfileStoreChain();

if (!profiles.TryGetAWSCredentials(profileName, out var credentials))
{
    throw new InvalidOperationException(
        $"AWS credential profile '{profileName}' was not found.");
}

var loggingConfiguration = new AWSLoggerConfig(
    RequiredSetting("CloudWatchLogs:LogGroup"))
{
    LogStreamName = RequiredSetting("CloudWatchLogs:LogStreamName"),
    Region = RequiredSetting("Aws:Region"),
    AuthenticationRegion = RequiredSetting("Aws:Region"),
    ServiceUrl = RequiredSetting("Aws:ServiceUrl"),
    Credentials = credentials,
    DisableLogGroupCreation = true,
    BatchPushInterval = TimeSpan.FromSeconds(5)
};

builder.Logging.AddAWSProvider(loggingConfiguration);

builder.Services.AddLearningServices();
builder.Services.AddLearningAws(
    builder.Configuration,
    useWorkerProfile: true);
builder.Services.AddHostedService<Worker>();

using var host = builder.Build();
await host.RunAsync();

string RequiredSetting(string key)
{
    var value = builder.Configuration[key];

    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException(
            $"Required configuration '{key}' is missing.");
    }

    return value;
}