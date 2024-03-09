using Bielu.Examine.Elasticsearch.Extensions;
using Bielu.Examine.Elasticsearch.Helpers;
using Bielu.Examine.Elasticsearch.Model;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Transport.Extensions;
using Lucene.Net.Documents;

namespace Bielu.Examine.Elasticsearch.Services;

public class ElasticsearchService(IElasticSearchClientFactory factory, IIndexStateService service) : IElasticsearchService
{
    public bool IndexExists(string examineIndexName, string indexAlias)
    {
        var state = service.GetIndexState(examineIndexName);
        var client = GetClient(examineIndexName);
        var aliasExists = client.Indices.Exists(indexAlias).Exists;
        if (aliasExists)
        {
            var indexesMappedToAlias = client.Indices.Get(indexAlias).Indices;
            if (indexesMappedToAlias.Count > 0)
            {
                state.Exist = true;

                return state.Exist;
            }
        }
        state.Exist = false;
        return state.Exist;
    }
    public IEnumerable<string>? GetCurrentIndexNames(string examineIndexName, string indexAlias)
    {
        return GetIndexesAssignedToAlias(GetClient(examineIndexName), indexAlias);
    }
    private static List<string>? GetIndexesAssignedToAlias(ElasticsearchClient client, string? aliasName)
    {
        var aliasExists = client.Indices.Exists(aliasName).Exists;
        if (aliasExists)
        {
            var indexesMappedToAlias = client.Indices.Get(aliasName).Indices;
            if (indexesMappedToAlias.Count > 0)
            {
                return indexesMappedToAlias?.Keys?.Select(x => x.ToString())?.ToList();
            }
        }

        return new List<string>();
    }
    private ElasticsearchClient GetClient(string examineIndexName)
    {
        return factory.GetOrCreateClient(examineIndexName);
    }
    public void EnsuredIndexExists(string examineIndexName, string? indexAlias = null) => throw new NotImplementedException();
    public void CreateIndex(string examineIndexName)
    {
        var state = service.GetIndexState(examineIndexName);
        if (state.CreatingNewIndex)
        {
            return;
        }

        state.CreatingNewIndex = true;

    }
    public Properties? GetProperties(string examineIndexName, string? indexAlias)
    {
        var client = GetClient(examineIndexName);
        var indexesMappedToAlias = GetIndexesAssignedToAlias(client, indexAlias).ToList();
        if (indexesMappedToAlias.Count <= 0)
        {
            return null;
        }
        GetMappingResponse response =
            client.Indices.GetMapping(mapping => mapping.Indices(indexesMappedToAlias[0]));
        return response.GetMappingFor(indexesMappedToAlias[0]).Properties;
    }
    public ElasticSearchSearchResults Search(string examineIndexName,SearchRequestDescriptor<ElasticDocument> searchDescriptor)
    {
        var client = GetClient(examineIndexName);
        SearchResponse<ElasticDocument>
            searchResult = client.Search<ElasticDocument>(searchDescriptor);
        return searchResult.ConvertToSearchResults();
    }
    public ElasticSearchSearchResults Search(string examineIndexName, SearchRequest<Document> searchDescriptor)
    {
        var client = GetClient(examineIndexName);
        SearchResponse<ElasticDocument>
            searchResult = client.Search<ElasticDocument>(searchDescriptor);
        return searchResult.ConvertToSearchResults();
    }
    private string PrepareIndexName()
    {
        if (_currentSuffix == string.Empty)
        {
            _currentSuffix = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        }
        return $"{IndexName}{_currentSuffix}".ToLowerInvariant();
    }
}
