using System;

namespace ServiceMonitor.Cloud
{
    public class InstanceProperty
    {
        public string Name { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }
        public string Type { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
}
