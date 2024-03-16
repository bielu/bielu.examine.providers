using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Bielu.Examine.Elasticsearch.Configuration;
using Bielu.Examine.Elasticsearch.Constants;

using Microsoft.Extensions.Options;

namespace Bielu.Examine.Elasticsearch.Services;

public class AzureSearchClientFactory : IAzureSearchClientFactory
{
    public SearchIndexClient GetOrCreateIndexClient(string? indexName) => throw new NotImplementedException();
    public SearchClient GetOrCreateSearchClient(string? indexName) => throw new NotImplementedException();
}
