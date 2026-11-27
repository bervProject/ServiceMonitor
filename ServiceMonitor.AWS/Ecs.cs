using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.ECS;
using Amazon.ECS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceMonitor.Cloud;

namespace ServiceMonitor.AWS;

public class Ecs(IServiceProvider serviceProvider, ILogger<Ecs> logger) : IContainerService
{
    private readonly ILogger<Ecs> _logger = logger;

    public async Task<ICollection<Cloud.Model.Cluster>> GetClusters(string region, CancellationToken cancellationToken = default)
    {
        // TODO: need better solution for client resolution
        var ecsRegion = RegionEndpoint.GetBySystemName(region);
        using var ecsClient = ActivatorUtilities.CreateInstance<AmazonECSClient>(serviceProvider, ecsRegion);
        if (ecsClient == null)
        {
            throw new ArgumentNullException(nameof(ecsClient));
        }
        return await GetClusters(ecsClient, cancellationToken);
    }

    internal static async Task<ICollection<Cloud.Model.Cluster>> GetClusters(IAmazonECS ecsClient, CancellationToken cancellationToken = default)
    {
        var clusters = new List<Cloud.Model.Cluster>();
        var request = new ListClustersRequest();
        do
        {
            var response = await ecsClient.ListClustersAsync(request, cancellationToken);
            foreach (var clusterArn in response.ClusterArns ?? Enumerable.Empty<string>())
            {
                var cluster = new Cloud.Model.Cluster { Identity = clusterArn };
                var servicesRequest = new ListServicesRequest { Cluster = clusterArn };
                do
                {
                    var services = await ecsClient.ListServicesAsync(servicesRequest, cancellationToken);
                    cluster.Services.AddRange((services.ServiceArns ?? Enumerable.Empty<string>())
                        .Select(arn => new Cloud.Model.Service { Identity = arn }));
                    servicesRequest.NextToken = services.NextToken;
                } while (!string.IsNullOrEmpty(servicesRequest.NextToken));

                var tasksRequest = new ListTasksRequest { Cluster = clusterArn };
                do
                {
                    var tasks = await ecsClient.ListTasksAsync(tasksRequest, cancellationToken);
                    cluster.Tasks.AddRange((tasks.TaskArns ?? Enumerable.Empty<string>())
                        .Select(arn => new Cloud.Model.Task { Identity = arn }));
                    tasksRequest.NextToken = tasks.NextToken;
                } while (!string.IsNullOrEmpty(tasksRequest.NextToken));

                clusters.Add(cluster);
            }
            request.NextToken = response.NextToken;
        } while (!string.IsNullOrEmpty(request.NextToken));

        return clusters;
    }
}