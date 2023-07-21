using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace ServiceMonitor.Cloud
{
    public interface IBucket
    {
        Task<ICollection<BasicProperty>> GetBuckets(string region, CancellationToken cancellationToken = default);
    }
}
