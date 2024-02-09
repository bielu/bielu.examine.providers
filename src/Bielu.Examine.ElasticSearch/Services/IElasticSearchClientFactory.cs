using Bielu.Examine.ElasticSearch.Configuration;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Options;

namespace Bielu.Examine.ElasticSearch.Services;

public interface IElasticSearchClientFactory
{
    ElasticsearchClient GetOrCreateClient(string? indexName);
}

public class ElasticSearchClientFactory : IElasticSearchClientFactory
{
    private BieluExamineElasticOptions _bieluExamineElasticOptions;
    Dictionary<string?, ElasticsearchClient> _clients = new Dictionary<string?, ElasticsearchClient>();

    public ElasticSearchClientFactory(IOptionsMonitor<BieluExamineElasticOptions> examineElasticOptions)
    {
        _bieluExamineElasticOptions = examineElasticOptions.CurrentValue;
        examineElasticOptions.OnChange(x => { _bieluExamineElasticOptions = x; });
    }

    public ElasticsearchClient GetOrCreateClient(string? indexName)
    {
        if (_clients.TryGetValue(indexName, out var value))
        {
            return value;
        }

        var indexConfiguration = _bieluExamineElasticOptions.IndexConfigurations.FirstOrDefault(x => x.Name == indexName);
        if (indexConfiguration == null)
        {
            throw new InvalidOperationException($"Index configuration for {indexName} not found");
        }

        var client = indexConfiguration.AuthenticationType switch
        {
            AuthenticationType.None => new ElasticsearchClient(new Uri(indexConfiguration.ConnectionString)),
            AuthenticationType.Cloud => new ElasticsearchClient(indexConfiguration.AuthenticationDetails.Id,
                new BasicAuthentication(indexConfiguration.AuthenticationDetails.Username,
                    indexConfiguration.AuthenticationDetails.Password)),
            AuthenticationType.CloudApi => new ElasticsearchClient(indexConfiguration.AuthenticationDetails.Id,
                new ApiKey(indexConfiguration.AuthenticationDetails.ApiKey)),
            _ => throw new InvalidOperationException("Invalid authentication type")
        };

        _clients.Add(indexName, client);
        return client;
    }
}
