using Bielu.Examine.ElasticSearch.Configuration;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Options;

namespace Bielu.Examine.ElasticSearch.Services;

public interface IElasticSearchClientFactory
{
    ElasticsearchClient GetOrCreateClient(string indexName);
}

public class ElasticSearchClientFactory : IElasticSearchClientFactory
{
    private ExamineElasticOptions _examineElasticOptions;
    Dictionary<string, ElasticsearchClient> _clients = new Dictionary<string, ElasticsearchClient>();

    public ElasticSearchClientFactory(IOptionsMonitor<ExamineElasticOptions> examineElasticOptions)
    {
        _examineElasticOptions = examineElasticOptions.CurrentValue;
        examineElasticOptions.OnChange(x => { _examineElasticOptions = x; });
    }

    public ElasticsearchClient GetOrCreateClient(string indexName)
    {
        if (_clients.ContainsKey(indexName))
        {
            return _clients[indexName];
        }

        var indexConfiguration = _examineElasticOptions.IndexConfigurations.FirstOrDefault(x => x.Name == indexName);
        if (indexConfiguration == null)
        {
            throw new Exception($"Index configuration for {indexName} not found");
        }

        var client = indexConfiguration.AuthenticationType switch
        {
            AuthenticationType.None => new ElasticsearchClient(new Uri(indexConfiguration.ConnectionString)),
            AuthenticationType.Cloud => new ElasticsearchClient(indexConfiguration.AuthenticationDetails.Id,
                new BasicAuthentication(indexConfiguration.AuthenticationDetails.Username,
                    indexConfiguration.AuthenticationDetails.Password)),
            AuthenticationType.CloudApi => new ElasticsearchClient(indexConfiguration.AuthenticationDetails.Id,
                new ApiKey(indexConfiguration.AuthenticationDetails.ApiKey)),
            _ => throw new Exception("Invalid authentication type")
        };

        _clients.Add(indexName, client);
        return client;
    }
}