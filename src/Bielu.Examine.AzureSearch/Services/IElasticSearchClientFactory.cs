
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;

namespace Bielu.Examine.Elasticsearch.Services;

public interface IAzureSearchClientFactory
{
    SearchIndexClient GetOrCreateIndexClient(string? indexName);
    SearchClient GetOrCreateSearchClient(string? indexName);
}
