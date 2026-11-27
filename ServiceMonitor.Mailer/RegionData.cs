namespace ServiceMonitor.Mailer;

public class RegionData<T>
{
    public string Region { get; set; } = string.Empty;
    public ICollection<T> Data { get; set; } = [];
}