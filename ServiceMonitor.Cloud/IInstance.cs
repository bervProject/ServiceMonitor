using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceMonitor.Cloud
{
    public interface IInstance
    {
        Task<ICollection<InstanceProperty>> GetInstancesAsync(string region, int limit = 100, CancellationToken cancellationToken = default);
    }
}