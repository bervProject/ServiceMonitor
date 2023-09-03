using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ServiceMonitor.Cloud.Model
{
    public class TagItem
    {
        public IReadOnlyCollection<string> Tags { get; set; }
        public DateTime CreatedTime { get; set; }
        public string ImageDigest { get; set; }
    }
}
