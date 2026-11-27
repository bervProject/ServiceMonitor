using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SimpleEmailV2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceMonitor.AWS;
using ServiceMonitor.Cloud;
using ServiceMonitor.Mailer;

await RunAsync();

static async Task RunAsync()
{
    var configuration = BuildConfiguration();
    using var serviceProvider = BuildServiceProvider(configuration);
    using var scope = serviceProvider.CreateScope();

    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Mailer");
    logger.LogDebug("Starting application");

    var mailer = scope.ServiceProvider.GetRequiredService<ReportMailer>();
    await mailer.SendUsageReportAsync(configuration["TO_EMAIL"], configuration["FROM_EMAIL"]);
}

static IConfiguration BuildConfiguration()
{
    return new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .Build();
}

static ServiceProvider BuildServiceProvider(IConfiguration configuration)
{
    var services = new ServiceCollection();
    services.AddSingleton(configuration);
    services.AddLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConfiguration(configuration.GetSection("Logging"));
        logging.AddConsole();
    });

    var awsOptions = new AWSOptions
    {
        Region = RegionEndpoint.GetBySystemName(ReportMailer.SesRegion)
    };
    services.AddDefaultAWSOptions(awsOptions);
    services.AddAWSService<IAmazonSimpleEmailServiceV2>();
    services.AddScoped<IInstance, AwsInstance>();
    services.AddScoped<IFunction, Lambda>();
    services.AddScoped<IImage, Ecr>();
    services.AddScoped<IBucket, S3>();
    services.AddScoped<ReportMailer>();

    return services.BuildServiceProvider();
}
