using Bielu.Examine.Elasticsearch.Configuration;
using Bielu.Examine.Elasticsearch.Constants;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Options;

namespace Bielu.Examine.Elasticsearch.Services;

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
        var defaultConfiguration = _bieluExamineElasticOptions.DefaultIndexConfiguration;
        var indexConfiguration = _bieluExamineElasticOptions.IndexConfigurations.FirstOrDefault(x => x.Name == indexName);
        if (indexConfiguration == null)
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
        if (_bieluExamineElasticOptions.DevMode)
        {
            connectionSettings.EnableDebugMode();
        }
        var client = new ElasticsearchClient(connectionSettings);
        _clients.Add(indexName, client);
        return client;
    }
}
