using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Logging;
using ServiceMonitor.Cloud;
using BasicProperty = ServiceMonitor.Cloud.Model.BasicProperty;
using FunctionProperty = ServiceMonitor.Cloud.Model.FunctionProperty;
using InstanceProperty = ServiceMonitor.Cloud.Model.InstanceProperty;

namespace ServiceMonitor.Mailer;

public sealed class ReportMailer(
    IInstance instances,
    IFunction functions,
    IImage images,
    IBucket buckets,
    IAmazonSimpleEmailServiceV2 emailClient,
    ILogger<ReportMailer> logger)
{
    public const string SesRegion = "ap-southeast-3";
    private const string TemplateName = "MyReport";
    private static readonly string[] Regions = ["ap-southeast-1", "ap-southeast-3"];

    public async Task<MailData> CreateMailDataAsync(CancellationToken cancellationToken = default)
    {
        var mailData = new MailData
        {
            ReportTime = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        foreach (var region in Regions)
        {
            logger.LogDebug("Collecting report data for region {Region}", region);
            mailData.EC2Services.Add(new RegionData<InstanceProperty>
            {
                Region = region,
                Data = await instances.GetInstancesAsync(region, cancellationToken: cancellationToken)
            });
            mailData.LambdaList.Add(new RegionData<FunctionProperty>
            {
                Region = region,
                Data = await functions.GetFunctionsAsync(region, cancellationToken)
            });
            mailData.ECRList.Add(new RegionData<BasicProperty>
            {
                Region = region,
                Data = await images.GetImagesAsync(region, cancellationToken)
            });
            mailData.S3List.Add(new RegionData<BasicProperty>
            {
                Region = region,
                Data = await buckets.GetBuckets(region, cancellationToken)
            });
        }

        return mailData;
    }

    public async Task SendUsageReportAsync(
        string? toEmail,
        string? fromEmail,
        CancellationToken cancellationToken = default)
    {
        var recipient = RequireEmailAddress(toEmail, "TO_EMAIL");
        var sender = RequireEmailAddress(fromEmail, "FROM_EMAIL");
        var mailData = await CreateMailDataAsync(cancellationToken);

        var response = await emailClient.SendEmailAsync(new SendEmailRequest
        {
            Content = new EmailContent
            {
                Template = new Template
                {
                    TemplateName = TemplateName,
                    TemplateData = JsonSerializer.Serialize(mailData)
                }
            },
            Destination = new Destination
            {
                ToAddresses = [recipient]
            },
            FromEmailAddress = sender
        }, cancellationToken);

        logger.LogInformation("Sent usage report with message ID {MessageId}", response.MessageId);
    }

    private static string RequireEmailAddress(string? address, string settingName)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidOperationException($"Required configuration value '{settingName}' is missing.");
        }

        return address.Trim();
    }
}