
using Amazon;
using Amazon.EC2.Model;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceMonitor.AWS;
using ServiceMonitor.Cloud;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

IConfiguration config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true)
    .AddEnvironmentVariables()
    .Build();

var serviceCollection = new ServiceCollection();
serviceCollection.AddSingleton(config);
serviceCollection.AddLogging(configure =>
{
    configure.ClearProviders();
    configure.AddConfiguration(config.GetSection("Logging"));
    configure.AddConsole();
});
serviceCollection.AddAWSService<IAmazonSimpleEmailServiceV2>();
serviceCollection.AddScoped<IAppRunner, AppRunner>();
serviceCollection.AddScoped<IInstance, AWSInstance>();
serviceCollection.AddScoped<IFunction, Lambda>();
serviceCollection.AddScoped<IImage, ECR>();
serviceCollection.AddScoped<IBucket, S3>();

var serviceProvider = serviceCollection.BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
logger.LogDebug("Starting application");

var regions = new List<string>
{
    "ap-southeast-1",
    "ap-southeast-3",
};

var containerImages = serviceProvider.GetRequiredService<IImage>();
foreach (var region in regions)
{
    var images = await containerImages.GetImagesAsync(region);
    foreach (var image in images)
    {
        var tags = await containerImages.GetTags(region, image.Name);
        var (success, failure) = await containerImages.DeleteTags(region, image.Name, tags);
        logger.LogInformation("{}:{}:{}", image.Name, JsonSerializer.Serialize(success, new JsonSerializerOptions
        {
            WriteIndented = true,
        }),
         JsonSerializer.Serialize(failure, new JsonSerializerOptions
         {
             WriteIndented = true,
         }));
    }
}
