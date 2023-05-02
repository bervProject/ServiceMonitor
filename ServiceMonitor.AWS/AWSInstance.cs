using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Microsoft.Extensions.DependencyInjection;
using ServiceMonitor.Cloud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceMonitor.AWS
{
    public class AWSInstance : IInstance
    {
        private readonly IServiceProvider _serviceProvider;
        public AWSInstance(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<ICollection<InstanceProperty>> GetInstancesAsync(string region, int limit = 100, CancellationToken cancellationToken = default)
        {
            // TODO: need better solution for client resolution
            var ec2Region = RegionEndpoint.GetBySystemName(region);
            using var ec2Client = ActivatorUtilities.CreateInstance<AmazonEC2Client>(_serviceProvider, ec2Region);
            if (ec2Client == null)
            {
                throw new ArgumentNullException(nameof(ec2Client));
            }
            var collection = new List<InstanceProperty>();
            var request = new DescribeInstancesRequest()
            {
                MaxResults = limit,
            };
            var nextToken = string.Empty;
            var response = await ec2Client.DescribeInstancesAsync(cancellationToken);

            response.Reservations.ForEach(x =>
            {
                var instances = x.Instances.Select(x => new InstanceProperty
                {
                    Name = x.InstanceId,
                    Type = x.InstanceType.Value,
                    CreatedAt = x.LaunchTime,
                    Status = x.State.Name.Value,
                });
                collection.AddRange(instances);
            });

            return collection;
        }
    }
}