using Elastic.Clients.Elasticsearch;

namespace Bielu.Examine.Elasticsearch.Services;

public interface IElasticSearchClientFactory
{
    ElasticsearchClient GetOrCreateClient(string? indexName);
}