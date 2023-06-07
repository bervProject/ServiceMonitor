using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace ServiceMonitor.Cloud
{
    public interface IImage
    {
        Task<ICollection<BasicProperty>> GetImagesAsync(string region, CancellationToken cancellationToken = default);
    }
}
