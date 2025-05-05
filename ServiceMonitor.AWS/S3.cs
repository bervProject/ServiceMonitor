using Amazon;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using ServiceMonitor.Cloud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceMonitor.AWS
{
    public class S3 : IBucket
    {
        private readonly IServiceProvider _serviceProvider;
        public S3(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public async Task<ICollection<BasicProperty>> GetBuckets(string region, CancellationToken cancellationToken = default)
        {
            var s3Region = RegionEndpoint.GetBySystemName(region);
            using var s3Client = ActivatorUtilities.CreateInstance<AmazonS3Client>(_serviceProvider, s3Region);
            if (s3Client == null)
            {
                throw new ArgumentNullException(nameof(s3Client));
            }
            var request = new Amazon.S3.Model.ListBucketsRequest();
            var response = await s3Client.ListBucketsAsync(request, cancellationToken);

            var list = response.Buckets.Select(x => new BasicProperty
            {
                Name = x.BucketName,
                CreatedAt = x.CreationDate ?? DateTime.Now,
                Status = "-",
            }).ToList();
            return list;
        }
    }
}
