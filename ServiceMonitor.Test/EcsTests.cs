using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.ECS;
using Amazon.ECS.Model;
using ServiceMonitor.AWS;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace ServiceMonitor.Test
{
    public class EcsTests
    {
        [Fact]
        public async Task ListsAllPagesSequentiallyForEachCluster()
        {
            using var client = new FakeEcsClient();
            using var cancellation = new CancellationTokenSource();
            client.ExpectedToken = cancellation.Token;

            var clusters = (await Ecs.GetClusters(client, cancellation.Token)).ToList();

            Assert.Equal(new[] { "cluster-1", "cluster-2" }, clusters.Select(x => x.Identity));
            Assert.Equal(new[] { "service-1", "service-2" }, clusters[0].Services.Select(x => x.Identity));
            Assert.Equal(new[] { "task-1", "task-2" }, clusters[0].Tasks.Select(x => x.Identity));
            Assert.Empty(clusters[1].Services);
            Assert.Empty(clusters[1].Tasks);
            Assert.Equal(new[] { "clusters:", "services:cluster-1:", "services:cluster-1:next", "tasks:cluster-1:", "tasks:cluster-1:next", "clusters:next", "services:cluster-2:", "tasks:cluster-2:" }, client.Calls);
        }

        private class FakeEcsClient : AmazonECSClient
        {
            public readonly List<string> Calls = new List<string>();
            public CancellationToken ExpectedToken;

            public override Task<ListClustersResponse> ListClustersAsync(ListClustersRequest request, CancellationToken cancellationToken = default)
            {
                Assert.Equal(ExpectedToken, cancellationToken);
                Calls.Add($"clusters:{request.NextToken}");
                return Task.FromResult(request.NextToken == null
                    ? new ListClustersResponse { ClusterArns = new List<string> { "cluster-1" }, NextToken = "next" }
                    : new ListClustersResponse { ClusterArns = new List<string> { "cluster-2" } });
            }

            public override Task<ListServicesResponse> ListServicesAsync(ListServicesRequest request, CancellationToken cancellationToken = default)
            {
                Assert.Equal(ExpectedToken, cancellationToken);
                Calls.Add($"services:{request.Cluster}:{request.NextToken}");
                if (request.Cluster == "cluster-2") return Task.FromResult(new ListServicesResponse());
                return Task.FromResult(request.NextToken == null
                    ? new ListServicesResponse { ServiceArns = new List<string> { "service-1" }, NextToken = "next" }
                    : new ListServicesResponse { ServiceArns = new List<string> { "service-2" } });
            }

            public override Task<ListTasksResponse> ListTasksAsync(ListTasksRequest request, CancellationToken cancellationToken = default)
            {
                Assert.Equal(ExpectedToken, cancellationToken);
                Calls.Add($"tasks:{request.Cluster}:{request.NextToken}");
                if (request.Cluster == "cluster-2") return Task.FromResult(new ListTasksResponse());
                return Task.FromResult(request.NextToken == null
                    ? new ListTasksResponse { TaskArns = new List<string> { "task-1" }, NextToken = "next" }
                    : new ListTasksResponse { TaskArns = new List<string> { "task-2" } });
            }
        }
    }
}