// See https://aka.ms/new-console-template for more information

using Amazon;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceMonitor.AWS;
using ServiceMonitor.Cloud;
using System.Text;

var serviceCollection = new ServiceCollection();
serviceCollection.AddLogging(configure =>
{
    configure.ClearProviders();
    configure.AddConsole();
});
serviceCollection.AddAWSService<IAmazonSimpleEmailServiceV2>();
serviceCollection.AddScoped<IAppRunner, AppRunner>();
serviceCollection.AddScoped<IInstance, AWSInstance>();

var serviceProvider = serviceCollection.BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
logger.LogDebug("Starting application");

var stringBuilder = new StringBuilder();

// get app runners
var appRunners = serviceProvider.GetRequiredService<IAppRunner>();
var data = await appRunners.GetAppRunnersAsync("ap-southeast-1");
stringBuilder.AppendLine("<h2>App Runner</h2>");
stringBuilder.AppendLine("<table>");
stringBuilder.AppendLine("<tr><th>Name</th><th>Status</th></tr>");
foreach (var appRunner in data)
{
    stringBuilder.AppendLine($"<tr><td>{appRunner.Name}</td><td>{appRunner.Status}</td></tr>");
}
stringBuilder.AppendLine("</table>");

// get instances
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
    stringBuilder.AppendLine("<tr><th>Name</th><th>Status</th></tr>");
    var ec2Instances = await instances.GetInstancesAsync(region);
    foreach (var ec2Instance in ec2Instances)
    {
        stringBuilder.AppendLine($"<tr><td>{ec2Instance.Name}</td><td>{ec2Instance.Status}</td></tr>");
    }
    stringBuilder.AppendLine("</table>");
}

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
