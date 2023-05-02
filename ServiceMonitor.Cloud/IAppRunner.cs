using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceMonitor.Cloud
{
    public interface IAppRunner
    {
        Task<ICollection<AppRunnerProperty>> GetAppRunnersAsync(string region, CancellationToken cancellationToken = default);
    }
}
