using ServiceMonitor.Cloud.Model;

namespace ServiceMonitor.Mailer;

public class MailData
{
    public string ReportTime { get; set; } = string.Empty;
    public List<RegionData<InstanceProperty>> EC2Services { get; set; } = [];
    public List<RegionData<FunctionProperty>> LambdaList { get; set; } = [];
    public List<RegionData<BasicProperty>> ECRList { get; set; } = [];
    public List<RegionData<BasicProperty>> S3List { get; set; } = [];
}