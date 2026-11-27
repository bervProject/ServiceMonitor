using System.Collections.Generic;

namespace ServiceMonitor.Cloud.Model;

public class Cluster
{
    public string Identity { get; set; }
    public List<Service> Services { get; private set; } = new List<Service>();
    public List<Task> Tasks { get; private set; } = new List<Task>();
}