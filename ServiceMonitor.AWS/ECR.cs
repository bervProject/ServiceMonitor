using Microsoft.Extensions.DependencyInjection;
using ServiceMonitor.Cloud;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.ECR;
using Amazon.ECR.Model;
using System.Linq;

namespace ServiceMonitor.AWS
{
    public class ECR : IImage
    {
        private readonly IServiceProvider _serviceProvider;
        public ECR(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public async Task<ICollection<BasicProperty>> GetImagesAsync(string region, CancellationToken cancellationToken = default)
        {
            // TODO: need better solution for client resolution
            var ecrRegion = RegionEndpoint.GetBySystemName(region);
            using var ecrClient = ActivatorUtilities.CreateInstance<AmazonECRClient>(_serviceProvider, ecrRegion);
            if (ecrClient == null)
            {
                throw new ArgumentNullException(nameof(ecrClient));
            }
            var request = new DescribeRepositoriesRequest()
            {
                MaxResults = 1000,
            };
            var nextToken = string.Empty;
            var response = await ecrClient.DescribeRepositoriesAsync(request, cancellationToken);

            var list = response.Repositories.Select(x => new BasicProperty
            {
                Name = x.RepositoryName,
                CreatedAt = x.CreatedAt,
                Status = "-",
            }).ToList();

            return list;
        }
    }
}
