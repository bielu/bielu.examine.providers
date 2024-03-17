using Bielu.Examine.Elasticsearch.Configuration;
using Bielu.Examine.Elasticsearch.Constants;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Options;

namespace Bielu.Examine.Elasticsearch.Services;

public class ElasticSearchClientFactory(IOptionsMonitor<BieluExamineElasticOptions> examineElasticOptions) : IElasticSearchClientFactory
{
    Dictionary<string?, ElasticsearchClient> _clients = new Dictionary<string?, ElasticsearchClient>();

    public ElasticsearchClient GetOrCreateClient(string? indexName)
    {
        if (_clients.TryGetValue(indexName, out var value))
        {
            return value;
        }
        var defaultConfiguration = examineElasticOptions.CurrentValue.DefaultIndexConfiguration;
        var indexConfiguration = examineElasticOptions.CurrentValue.IndexConfigurations.FirstOrDefault(x => x.Name == indexName);
        if (indexConfiguration == null || !indexConfiguration.OverrideClientConfiguration)
        {
            indexConfiguration = defaultConfiguration;
            indexName = BieluExamineElasticConstants.DefaultClient;
        }
        if (_clients.TryGetValue(indexName, out value))
        {
            return value;
        }
        var connectionSettings = indexConfiguration.AuthenticationType switch
        {
            AuthenticationType.None => new ElasticsearchClientSettings(new Uri(indexConfiguration.ConnectionString)),
            AuthenticationType.Cloud => new ElasticsearchClientSettings(indexConfiguration.AuthenticationDetails?.Id,
                new BasicAuthentication(indexConfiguration.AuthenticationDetails?.Username,
                    indexConfiguration.AuthenticationDetails?.Password)),
            AuthenticationType.CloudApi => new ElasticsearchClientSettings(indexConfiguration.AuthenticationDetails?.Id,
                new ApiKey(indexConfiguration.AuthenticationDetails?.ApiKey)),
            _ => throw new InvalidOperationException("Invalid authentication type")
        };
        if (examineElasticOptions.CurrentValue.DevMode)
        {
            connectionSettings.EnableDebugMode();
        }
        var client = new ElasticsearchClient(connectionSettings);
        _clients.Add(indexName, client);
        return client;
    }
}
