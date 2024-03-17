using Azure;
using Azure.Core;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Bielu.Examine.Core.Services;
using Bielu.Examine.Elasticsearch.Configuration;
using Bielu.Examine.Elasticsearch.Constants;
using Microsoft.Extensions.Options;

namespace Bielu.Examine.Elasticsearch.Services;

public class AzureSearchClientFactory(IOptionsMonitor<BieluExamineAzureSearchOptions> examineElasticOptions, IIndexStateService indexStateService) : IAzureSearchClientFactory
{
    Dictionary<string?, (SearchIndexClient IndexingClient, SearchClient searchClient)> _clients = new Dictionary<string?, (SearchIndexClient IndexingClient, SearchClient searchClient)>();

    public SearchIndexClient GetOrCreateIndexClient(string? indexName)
    {
        if (_clients.TryGetValue(indexName, out var value))
        {
            return value.IndexingClient;
        }
        var defaultConfiguration = examineElasticOptions.CurrentValue.DefaultIndexConfiguration;
        var indexConfiguration = examineElasticOptions.CurrentValue.IndexConfigurations.FirstOrDefault(x => x.Name == indexName);
        if (indexConfiguration == null)
        {
            indexConfiguration = defaultConfiguration;
        }
        else if (!indexConfiguration.OverrideClientConfiguration)
        {
            indexConfiguration = defaultConfiguration;
        }
        var client = CreateIndexClient(indexName, indexConfiguration);
        return client.IndexingClient;
    }
    private (SearchIndexClient IndexingClient, SearchClient searchClient) CreateIndexClient(string? indexName, IndexConfiguration indexConfiguration)
    {
        (SearchIndexClient IndexingClient, SearchClient searchClient) client;
        var indexingClientOptions = indexConfiguration.AuthenticationDetails.AuthenticationType switch
        {
            AuthenticationType.AzureKeyCredential => new SearchIndexClient(new Uri(indexConfiguration.AuthenticationDetails.EndPoint), new AzureKeyCredential(indexConfiguration.AuthenticationDetails.AdminApiKey)),
            AuthenticationType.TokenCredential => throw new NotImplementedException("TokenCredential is not implemented yet"),
            _ => throw new InvalidOperationException("Invalid authentication type")
        };
        var getState = indexStateService.GetIndexState(indexName);
        var searchClientOptions = indexConfiguration.AuthenticationDetails.AuthenticationType switch
        {
            AuthenticationType.AzureKeyCredential => new SearchClient(new Uri(indexConfiguration.AuthenticationDetails.EndPoint), getState.CurrentIndexName, new AzureKeyCredential(indexConfiguration.AuthenticationDetails.QueryApiKey)),
            AuthenticationType.TokenCredential => throw new NotImplementedException("TokenCredential is not implemented yet"),
            _ => throw new InvalidOperationException("Invalid authentication type")
        };
        client = (indexingClientOptions, searchClientOptions);
        _clients.Add(indexName, client);
        return client;
    }

    public SearchClient GetOrCreateSearchClient(string? indexName) {
        if (_clients.TryGetValue(indexName, out var value))
        {
            return value.searchClient;
        }
        var defaultConfiguration = examineElasticOptions.CurrentValue.DefaultIndexConfiguration;
        var indexConfiguration = examineElasticOptions.CurrentValue.IndexConfigurations.FirstOrDefault(x => x.Name == indexName);
        if (indexConfiguration == null)
        {
            indexConfiguration = defaultConfiguration;
        }
        else if (!indexConfiguration.OverrideClientConfiguration)
        {
            indexConfiguration = defaultConfiguration;
        }
        var client = CreateIndexClient(indexName, indexConfiguration);
        return client.searchClient;
    }
}
