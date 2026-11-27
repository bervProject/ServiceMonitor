using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceMonitor.Cloud;

public interface IContainerService
{
    public Task<ICollection<Model.Cluster>> GetClusters(string region, CancellationToken cancellationToken = default);

}