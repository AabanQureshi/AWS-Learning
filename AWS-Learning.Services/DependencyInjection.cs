using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.SQS;
using AWS_Learning.Configuration;
using AWS_Learning.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AWS_Learning;

public static class DependencyInjection
{
    public static IServiceCollection AddLearningServices(this IServiceCollection services)
    {
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IStorageService, StorageService>();
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IQueueService, QueueService>();
        services.AddScoped<IProcessingService, ProcessingService>();
        return services;
    }

    public static IServiceCollection AddLearningAws(
        this IServiceCollection services,
        IConfiguration configuration,
        bool useWorkerProfile = false)
    {
        services.AddOptions<AWSConfig>()
            .Bind(configuration.GetSection("Aws"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IAmazonS3>(provider =>
        {
            var config = provider.GetRequiredService<IOptions<AWSConfig>>().Value;
            return new AmazonS3Client(
                LoadCredentials(useWorkerProfile ? config.WorkerProfile : config.ApiProfile),
                new AmazonS3Config
                {
                    ServiceURL = config.ServiceUrl,
                    AuthenticationRegion = config.Region,
                    ForcePathStyle = true
                });
        });

        services.AddSingleton<IAmazonSQS>(provider =>
        {
            var config = provider.GetRequiredService<IOptions<AWSConfig>>().Value;
            return new AmazonSQSClient(
                LoadCredentials(useWorkerProfile ? config.WorkerProfile : config.ApiProfile),
                new AmazonSQSConfig
                {
                    ServiceURL = config.ServiceUrl,
                    AuthenticationRegion = config.Region
                });
        });

        services.AddSingleton<IAmazonDynamoDB>(provider =>
        {
            var config = provider.GetRequiredService<IOptions<AWSConfig>>().Value;
            return new AmazonDynamoDBClient(
                LoadCredentials(useWorkerProfile ? config.WorkerProfile : config.ApiProfile),
                new AmazonDynamoDBConfig
                {
                    ServiceURL = config.ServiceUrl,
                    AuthenticationRegion = config.Region
                });
        });

        return services;
    }

    private static AWSCredentials LoadCredentials(string profileName)
    {
        var profiles = new CredentialProfileStoreChain();
        if (!profiles.TryGetAWSCredentials(profileName, out var credentials))
        {
            throw new InvalidOperationException(
                $"AWS credential profile '{profileName}' was not found.");
        }

        return credentials;
    }
}
