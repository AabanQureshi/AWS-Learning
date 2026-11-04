using Amazon.Runtime.CredentialManagement;
using AWS.Logger;
using AWS_Learning;

var builder = WebApplication.CreateBuilder(args);

var profileName = RequiredSetting("Aws:ApiProfile");
var profiles = new CredentialProfileStoreChain();

if (!profiles.TryGetAWSCredentials(profileName, out var loggingCredentials))
{
    throw new InvalidOperationException(
        $"AWS credential profile '{profileName}' was not found.");
}

// Reuse the application endpoint so the instance override also applies to logs.
builder.Logging.AddAWSProvider(new AWSLoggerConfig(
    RequiredSetting("CloudWatchLogs:LogGroup"))
{
    LogStreamName = RequiredSetting("CloudWatchLogs:LogStreamName"),
    Region = RequiredSetting("Aws:Region"),
    AuthenticationRegion = RequiredSetting("Aws:Region"),
    ServiceUrl = RequiredSetting("Aws:ServiceUrl"),
    Credentials = loggingCredentials,
    DisableLogGroupCreation = true,
    BatchPushInterval = TimeSpan.FromSeconds(5)
});

builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks();
builder.Services.AddLearningServices();
builder.Services.AddLearningAws(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapStaticAssets();
app.MapControllers();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

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
