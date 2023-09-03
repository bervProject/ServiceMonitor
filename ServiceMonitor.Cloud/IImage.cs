using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using ServiceMonitor.Cloud.Model;

namespace ServiceMonitor.Cloud
{
    public interface IImage
    {
        Task<ICollection<BasicProperty>> GetImagesAsync(string region, CancellationToken cancellationToken = default);
        Task<ICollection<TagItem>> GetTags(string region, string repoName, CancellationToken cancellationToken = default);
        Task<(List<string>, List<string>)> DeleteTags(string region, string repoName, ICollection<TagItem> tags, CancellationToken cancellationToken = default);
    }
}
