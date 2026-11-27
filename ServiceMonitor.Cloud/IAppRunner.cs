using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceMonitor.Cloud.Model;

namespace ServiceMonitor.Cloud;

[Obsolete]
public interface IAppRunner
{
    Task<ICollection<AppRunnerProperty>> GetAppRunnersAsync(string region, CancellationToken cancellationToken = default);
}