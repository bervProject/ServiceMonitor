using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceMonitor.Cloud;
using ServiceMonitor.Mailer;
using BasicProperty = ServiceMonitor.Cloud.Model.BasicProperty;
using FunctionProperty = ServiceMonitor.Cloud.Model.FunctionProperty;
using InstanceProperty = ServiceMonitor.Cloud.Model.InstanceProperty;
using TagItem = ServiceMonitor.Cloud.Model.TagItem;
using Xunit;

namespace ServiceMonitor.Test;

public class ReportMailerTests
{
    [Fact]
    public async Task CreateMailDataAsyncCollectsEveryResourceForBothRegions()
    {
        var resources = new FakeCloudResources();
        using var emailClient = new FakeSesClient();
        var mailer = CreateMailer(resources, emailClient);

        var mailData = await mailer.CreateMailDataAsync();

        Assert.Equal(2, mailData.EC2Services.Count);
        Assert.Equal(2, mailData.LambdaList.Count);
        Assert.Equal(2, mailData.ECRList.Count);
        Assert.Equal(2, mailData.S3List.Count);
        Assert.Equal(new[] { "ap-southeast-1", "ap-southeast-3" }, mailData.EC2Services.Select(region => region.Region));
        Assert.All(mailData.EC2Services, region => Assert.Single(region.Data));
        Assert.All(mailData.LambdaList, region => Assert.Single(region.Data));
        Assert.All(mailData.ECRList, region => Assert.Single(region.Data));
        Assert.All(mailData.S3List, region => Assert.Single(region.Data));
        Assert.Equal(TimeSpan.Zero, DateTimeOffset.Parse(mailData.ReportTime).Offset);
    }

    [Fact]
    public async Task CreateMailDataAsyncRetainsRegionsWhenServicesReturnNoResults()
    {
        var resources = new FakeCloudResources(returnEmpty: true);
        using var emailClient = new FakeSesClient();
        var mailer = CreateMailer(resources, emailClient);

        var mailData = await mailer.CreateMailDataAsync();

        Assert.All(mailData.EC2Services, region => Assert.Empty(region.Data));
        Assert.All(mailData.LambdaList, region => Assert.Empty(region.Data));
        Assert.All(mailData.ECRList, region => Assert.Empty(region.Data));
        Assert.All(mailData.S3List, region => Assert.Empty(region.Data));
        Assert.Equal(2, mailData.EC2Services.Count);
        Assert.Equal(2, mailData.LambdaList.Count);
        Assert.Equal(2, mailData.ECRList.Count);
        Assert.Equal(2, mailData.S3List.Count);
    }

    [Fact]
    public async Task SendUsageReportAsyncSendsMailDataThroughSesTemplate()
    {
        var resources = new FakeCloudResources();
        using var emailClient = new FakeSesClient();
        var mailer = CreateMailer(resources, emailClient);

        await mailer.SendUsageReportAsync(" recipient@example.com ", " sender@example.com ");

        Assert.NotNull(emailClient.Request);
        Assert.Equal("sender@example.com", emailClient.Request.FromEmailAddress);
        Assert.Equal(new[] { "recipient@example.com" }, emailClient.Request.Destination.ToAddresses);
        Assert.Equal("MyReport", emailClient.Request.Content.Template.TemplateName);
        using var templateData = JsonDocument.Parse(emailClient.Request.Content.Template.TemplateData);
        var report = templateData.RootElement;
        Assert.True(report.TryGetProperty("ReportTime", out _));
        Assert.Equal(2, report.GetProperty("EC2Services").GetArrayLength());
        Assert.Equal(2, report.GetProperty("LambdaList").GetArrayLength());
        Assert.Equal(2, report.GetProperty("ECRList").GetArrayLength());
        Assert.Equal(2, report.GetProperty("S3List").GetArrayLength());
        Assert.Equal(8, resources.CallCount);
    }

    [Theory]
    [InlineData(null, "sender@example.com", "TO_EMAIL")]
    [InlineData(" ", "sender@example.com", "TO_EMAIL")]
    [InlineData("recipient@example.com", null, "FROM_EMAIL")]
    [InlineData("recipient@example.com", " ", "FROM_EMAIL")]
    public async Task SendUsageReportAsyncRejectsMissingAddressesWithoutAwsCalls(
        string recipient,
        string sender,
        string settingName)
    {
        var resources = new FakeCloudResources();
        using var emailClient = new FakeSesClient();
        var mailer = CreateMailer(resources, emailClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mailer.SendUsageReportAsync(recipient, sender));

        Assert.Contains(settingName, exception.Message);
        Assert.Equal(0, resources.CallCount);
        Assert.Null(emailClient.Request);
    }

    private static ReportMailer CreateMailer(FakeCloudResources resources, FakeSesClient emailClient)
    {
        return new ReportMailer(
            resources,
            resources,
            resources,
            resources,
            emailClient,
            NullLogger<ReportMailer>.Instance);
    }

    private sealed class FakeCloudResources(bool returnEmpty = false) : IInstance, IFunction, IImage, IBucket
    {
        public int CallCount { get; private set; }

        public Task<ICollection<InstanceProperty>> GetInstancesAsync(
            string region,
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            ICollection<InstanceProperty> resources = returnEmpty
                ? []
                : [new InstanceProperty { Name = $"instance-{region}", Type = "t3.micro", Status = "running" }];
            return Task.FromResult(resources);
        }

        public Task<ICollection<FunctionProperty>> GetFunctionsAsync(string region, CancellationToken cancellationToken = default)
        {
            CallCount++;
            ICollection<FunctionProperty> resources = returnEmpty
                ? []
                : [new FunctionProperty { Name = $"function-{region}", Version = "1", Runtime = "dotnet", Status = "active" }];
            return Task.FromResult(resources);
        }

        public Task<ICollection<BasicProperty>> GetImagesAsync(string region, CancellationToken cancellationToken = default)
        {
            CallCount++;
            ICollection<BasicProperty> resources = returnEmpty
                ? []
                : [new BasicProperty { Name = $"image-{region}", Status = "available" }];
            return Task.FromResult(resources);
        }

        public Task<ICollection<BasicProperty>> GetBuckets(string region, CancellationToken cancellationToken = default)
        {
            CallCount++;
            ICollection<BasicProperty> resources = returnEmpty
                ? []
                : [new BasicProperty { Name = $"bucket-{region}", Status = "available" }];
            return Task.FromResult(resources);
        }

        public Task<ICollection<TagItem>> GetTags(string region, string repoName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ICollection<TagItem>>([]);
        }

        public Task<(List<string>, List<string>)> DeleteTags(
            string region,
            string repoName,
            ICollection<TagItem> tags,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult((new List<string>(), new List<string>()));
        }
    }

    private sealed class FakeSesClient : AmazonSimpleEmailServiceV2Client
    {
        public SendEmailRequest Request { get; private set; }

        public override Task<SendEmailResponse> SendEmailAsync(
            SendEmailRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(new SendEmailResponse { MessageId = "message-id" });
        }
    }
}