using System;
using System.Collections.Generic;

namespace ServiceMonitor.Cloud.Model;

public class TagItem
{
    public IReadOnlyCollection<string> Tags { get; set; }
    public DateTime CreatedTime { get; set; }
    public string ImageDigest { get; set; }
}