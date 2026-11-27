using Microsoft.Extensions.DependencyInjection;
using ServiceMonitor.Cloud;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using System.Linq;
using ServiceMonitor.Cloud.Model;

namespace ServiceMonitor.AWS;

public class Lambda(IServiceProvider serviceProvider) : IFunction
{
    public async Task<ICollection<FunctionProperty>> GetFunctionsAsync(string region, CancellationToken cancellationToken = default)
    {
        var lambdaRegion = RegionEndpoint.GetBySystemName(region);
        using var lambdaClient = ActivatorUtilities.CreateInstance<AmazonLambdaClient>(serviceProvider, lambdaRegion);
        if (lambdaClient == null)
        {
            throw new ArgumentNullException(nameof(lambdaClient));
        }
        var collection = new List<InstanceProperty>();
        var request = new ListFunctionsRequest()
        {
            MaxItems = 50,
            FunctionVersion = FunctionVersion.ALL,
        };
        var nextToken = string.Empty;
        var response = await lambdaClient.ListFunctionsAsync(request, cancellationToken);

        return response.Functions.Select(x => new FunctionProperty
        {
            Status = string.Empty,
            Name = x.FunctionName,
            Version = x.Version,
            Runtime = string.IsNullOrEmpty(x.Runtime) ? string.Empty : x.Runtime,
            CreatedAt = DateTimeOffset.Parse(x.LastModified),
        }).ToList();

    }
}