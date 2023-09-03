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
using ServiceMonitor.Cloud.Model;
using Microsoft.Extensions.Logging;

namespace ServiceMonitor.AWS
{
    public class ECR : IImage
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ECR> _logger;

        public ECR(IServiceProvider serviceProvider, ILogger<ECR> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
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

        public async Task<ICollection<TagItem>> GetTags(string region, string repoName, CancellationToken cancellationToken = default)
        {
            var ecrRegion = RegionEndpoint.GetBySystemName(region);
            using var ecrClient = ActivatorUtilities.CreateInstance<AmazonECRClient>(_serviceProvider, ecrRegion);
            if (ecrClient == null)
            {
                throw new ArgumentNullException(nameof(ecrClient));
            }
            var imageListRequest = new DescribeImagesRequest
            {
                RepositoryName = repoName,
                Filter = new Amazon.ECR.Model.DescribeImagesFilter
                {
                    TagStatus = TagStatus.TAGGED,
                }
            };
            var tagsResources = await ecrClient.DescribeImagesAsync(imageListRequest);
            return tagsResources.ImageDetails.Select(x => new TagItem
            {
                CreatedTime = x.ImagePushedAt,
                Tags = x.ImageTags,
                ImageDigest = x.ImageDigest,
            }).ToList();
        }

        public async Task<(List<string>, List<string>)> DeleteTags(string region, string repoName, ICollection<TagItem> tags, CancellationToken cancellationToken = default)
        {
            var sample = tags.FirstOrDefault();
            if (sample == null)
            {
                return (new List<string>(), new List<string>());
            }

            var ecrRegion = RegionEndpoint.GetBySystemName(region);
            using var ecrClient = ActivatorUtilities.CreateInstance<AmazonECRClient>(_serviceProvider, ecrRegion);
            if (ecrClient == null)
            {
                throw new ArgumentNullException(nameof(ecrClient));
            }

            var isShaMode = sample.Tags.Contains("sha-");
            var deleteImages = new List<ImageIdentifier>();
            if (isShaMode)
            {
                foreach (var tag in tags)
                {
                    if (tag.Tags.Contains("main") || tag.Tags.Contains("latest") || tag.Tags.Contains("master"))
                    {
                        continue;
                    }
                    deleteImages.Add(new ImageIdentifier
                    {
                        ImageDigest = tag.ImageDigest,
                    });
                }
            }
            else
            {
                var sorted = tags.OrderByDescending(x => x.CreatedTime).Select(x => new ImageIdentifier
                {
                    ImageDigest = x.ImageDigest,
                }).ToList();
                sorted.RemoveAt(0);
                deleteImages.AddRange(sorted);
            }

            if (deleteImages.Count == 0)
            {
                return (new List<string>(), new List<string>());
            }

            _logger.LogInformation("Will remove {} tags", deleteImages.Count);

            var deleteImageRequest = new BatchDeleteImageRequest
            {
                RepositoryName = repoName,
                ImageIds = deleteImages,
            };

            var batchResponse = await ecrClient.BatchDeleteImageAsync(deleteImageRequest);

            _logger.LogInformation("Success remove {} and fail {}", batchResponse.ImageIds.Count, batchResponse.Failures.Count);

            return (batchResponse.ImageIds.Select(x => x.ImageTag).ToList(), batchResponse.Failures.Select(x => x.ImageId.ImageTag).ToList());
        }
    }
}
