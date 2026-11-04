using Amazon.CloudWatch;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.SQS;
using AWS.Logger;
using AWS_Learning.Monitoring.Worker;
using AWS_Learning.Monitoring.Worker.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

var profileName = RequiredSetting("Monitoring:Profile");
var profiles = new CredentialProfileStoreChain();

if (!profiles.TryGetAWSCredentials(profileName, out var credentials))
{
    throw new InvalidOperationException(
        $"AWS credential profile '{profileName}' was not found.");
}

// Keep log publication on the same endpoint and identity as metric collection.
builder.Logging.AddAWSProvider(new AWSLoggerConfig(
    RequiredSetting("CloudWatchLogs:LogGroup"))
{
    LogStreamName = RequiredSetting("CloudWatchLogs:LogStreamName"),
    Region = RequiredSetting("Monitoring:Region"),
    AuthenticationRegion = RequiredSetting("Monitoring:Region"),
    ServiceUrl = RequiredSetting("Monitoring:ServiceUrl"),
    Credentials = credentials,
    DisableLogGroupCreation = true,
    BatchPushInterval = TimeSpan.FromSeconds(5)
});

builder.Services
    .AddOptions<MonitoringOptions>()
    .Bind(builder.Configuration.GetSection(MonitoringOptions.SectionName))
    .Validate(options => IsHttpUrl(options.ServiceUrl),
        "Monitoring:ServiceUrl must be an absolute HTTP or HTTPS URL.")
    .Validate(options => IsHttpUrl(options.QueueUrl),
        "Monitoring:QueueUrl must be an absolute HTTP or HTTPS URL.")
    .Validate(options =>
        !string.IsNullOrWhiteSpace(options.Region) &&
        !string.IsNullOrWhiteSpace(options.Profile) &&
        !string.IsNullOrWhiteSpace(options.Namespace) &&
        !string.IsNullOrWhiteSpace(options.MetricName),
        "Monitoring region, profile, namespace and metric name are required.")
    .Validate(options => options.IntervalSeconds >= 1,
        "Monitoring:IntervalSeconds must be positive.")
    .ValidateOnStart();

builder.Services.AddSingleton<AWSCredentials>(credentials);

builder.Services.AddSingleton<IAmazonSQS>(provider =>
{
    var options = provider
        .GetRequiredService<IOptions<MonitoringOptions>>().Value;

    return new AmazonSQSClient(
        provider.GetRequiredService<AWSCredentials>(),
        new AmazonSQSConfig
        {
            ServiceURL = options.ServiceUrl,
            AuthenticationRegion = options.Region
        });
});

builder.Services.AddSingleton<IAmazonCloudWatch>(provider =>
{
    var options = provider
        .GetRequiredService<IOptions<MonitoringOptions>>().Value;

    return new AmazonCloudWatchClient(
        provider.GetRequiredService<AWSCredentials>(),
        new AmazonCloudWatchConfig
        {
            ServiceURL = options.ServiceUrl,
            AuthenticationRegion = options.Region
        });
});

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

static bool IsHttpUrl(string? value)
{
    return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
           (uri.Scheme == Uri.UriSchemeHttp ||
            uri.Scheme == Uri.UriSchemeHttps);
}
