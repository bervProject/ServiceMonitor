using Microsoft.Extensions.DependencyInjection;
using ServiceMonitor.Cloud;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.AppRunner;
using Amazon.AppRunner.Model;
using System.Linq;

namespace ServiceMonitor.AWS
{
    public class AppRunner : IAppRunner
    {
        private readonly IServiceProvider _serviceProvider;
        public AppRunner(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public async Task<ICollection<AppRunnerProperty>> GetAppRunnersAsync(string region, CancellationToken cancellationToken = default)
        {
            var appRunnerRegion = RegionEndpoint.GetBySystemName(region);
            using var appRunnerClient = ActivatorUtilities.CreateInstance<AmazonAppRunnerClient>(_serviceProvider, appRunnerRegion);
            if (appRunnerClient == null)
            {
                throw new ArgumentNullException(nameof(appRunnerClient));
            }
            var collection = new List<InstanceProperty>();
            var request = new ListServicesRequest()
            {
                MaxResults = 20,
            };
            var nextToken = string.Empty;
            var response = await appRunnerClient.ListServicesAsync(request, cancellationToken);

            return response.ServiceSummaryList.Select(x => new AppRunnerProperty
            {
                Status = x.Status,
                Name = x.ServiceName,
            }).ToList();
        }
    }
}
