// See https://aka.ms/new-console-template for more information

using Amazon;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceMonitor.AWS;
using ServiceMonitor.Cloud;
using System.Text;

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

var stringBuilder = new StringBuilder();

// get app runners
logger.LogDebug("Populate App Runners");
var appRunners = serviceProvider.GetRequiredService<IAppRunner>();
var data = await appRunners.GetAppRunnersAsync("ap-southeast-1");
stringBuilder.AppendLine("<h2>App Runner</h2>");
stringBuilder.AppendLine("<table>");
stringBuilder.AppendLine("<tr><th>Name</th><th>Created At</th><th>Status</th></tr>");
foreach (var appRunner in data)
{
    stringBuilder.AppendLine($"<tr><td>{appRunner.Name}</td><td>{appRunner.CreatedAt}</td><td>{appRunner.Status}</td></tr>");
}
stringBuilder.AppendLine("</table>");

// get instances
logger.LogDebug("Populate EC2 Instances");
var instances = serviceProvider.GetRequiredService<IInstance>();

var regions = new List<string>
{
    "ap-southeast-1",
    "ap-southeast-3",
};
foreach (var region in regions)
{
    stringBuilder.AppendLine($"<br><h2>EC2 Instances (Region: {region})</h2>");
    stringBuilder.AppendLine("<table>");
    stringBuilder.AppendLine("<tr><th>Name</th><th>Created At</th><th>Type</th><th>Status</th></tr>");
    var ec2Instances = await instances.GetInstancesAsync(region);
    foreach (var ec2Instance in ec2Instances)
    {
        stringBuilder.AppendLine($"<tr><td>{ec2Instance.Name}</td><td>{ec2Instance.CreatedAt}</td><td>{ec2Instance.Type}</td><td>{ec2Instance.Status}</td></tr>");
    }
    stringBuilder.AppendLine("</table>");
}

// get lambda
logger.LogDebug("Populate Lambda");
var lambdaFunction = serviceProvider.GetRequiredService<IFunction>();
foreach (var region in regions)
{
    stringBuilder.AppendLine($"<br><h2>Lambda (Region: {region})</h2>");
    stringBuilder.AppendLine("<table>");
    stringBuilder.AppendLine("<tr><th>Name</th><th>Created At</th><th>Version</th><th>Status</th></tr>");
    var functions = await lambdaFunction.GetFunctionsAsync(region);
    foreach (var functionData in functions)
    {
        stringBuilder.AppendLine($"<tr><td>{functionData.Name}</td><td>{functionData.CreatedAt}</td><td>{functionData.Version}</td><td>{functionData.Status}</td></tr>");
    }
    stringBuilder.AppendLine("</table>");
}

logger.LogDebug("Populate ECR");
var containerImages = serviceProvider.GetRequiredService<IImage>();
foreach (var region in regions)
{
    stringBuilder.AppendLine($"<br><h2>ECR (Region: {region})</h2>");
    stringBuilder.AppendLine("<table>");
    stringBuilder.AppendLine("<tr><th>Name</th><th>Created At</th><th>Status</th></tr>");
    var images = await containerImages.GetImagesAsync(region);
    foreach (var imageData in images)
    {
        stringBuilder.AppendLine($"<tr><td>{imageData.Name}</td><td>{imageData.CreatedAt}</td><td>{imageData.Status}</td></tr>");
    }
    stringBuilder.AppendLine("</table>");
}

logger.LogDebug("Populate S3");
var buckets = serviceProvider.GetRequiredService<IBucket>();
foreach (var region in regions)
{
    stringBuilder.AppendLine($"<br><h2>S3 (Region: {region})</h2>");
    stringBuilder.AppendLine("<table>");
    stringBuilder.AppendLine("<tr><th>Name</th><th>Created At</th><th>Status</th></tr>");
    var bucketList = await buckets.GetBuckets(region);
    foreach (var bucket in bucketList)
    {
        stringBuilder.AppendLine($"<tr><td>{bucket.Name}</td><td>{bucket.CreatedAt}</td><td>{bucket.Status}</td></tr>");
    }
    stringBuilder.AppendLine("</table>");
}
// sending email
logger.LogDebug("Sending Email");
var sesRegion = RegionEndpoint.GetBySystemName("ap-southeast-1");
using var emailClient = new AmazonSimpleEmailServiceV2Client(sesRegion);
var request = new SendEmailRequest
{
    Content = new EmailContent
    {
        Simple = new Message
        {
            Subject = new Content
            {
                Data = "Report Usage"
            },
            Body = new Body
            {
                Html = new Content
                {
                    Data = stringBuilder.ToString()
                }
            }
        }
    },
    Destination = new Destination
    {
        ToAddresses = new List<string> { Environment.GetEnvironmentVariable("TO_EMAIL") ?? "" },
    },
    FromEmailAddress = Environment.GetEnvironmentVariable("FROM_EMAIL") ?? ""
};
var response = await emailClient.SendEmailAsync(request);
logger.LogDebug($"Sent Email: {response.MessageId}");
