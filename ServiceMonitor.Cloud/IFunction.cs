using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace ServiceMonitor.Cloud
{
    public interface IFunction
    {
        Task<ICollection<FunctionProperty>> GetFunctionsAsync(string region, CancellationToken cancellationToken = default);
    }
}
