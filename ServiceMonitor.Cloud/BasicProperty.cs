using System;

namespace ServiceMonitor.Cloud
{
    public class BasicProperty
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
