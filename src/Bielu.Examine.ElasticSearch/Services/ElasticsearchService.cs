using Bielu.Examine.Elasticsearch.Extensions;
using Bielu.Examine.Elasticsearch.Helpers;
using Bielu.Examine.Elasticsearch.Model;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Transport.Extensions;
using Examine;
using Lucene.Net.Documents;

namespace Bielu.Examine.Elasticsearch.Services;

public class ElasticsearchService(IElasticSearchClientFactory factory, IIndexStateService service) : IElasticsearchService
{
    public bool IndexExists(string examineIndexName)
    {
        var state = service.GetIndexState(examineIndexName);
        var client = GetClient(examineIndexName);
        var aliasExists = client.Indices.Exists(state.IndexAlias).Exists;
        if (aliasExists)
        {
            var indexesMappedToAlias = client.Indices.Get(state.IndexAlias).Indices;
            if (indexesMappedToAlias.Count > 0)
            {
                state.Exist = true;

                return state.Exist;
            }
        }
        state.Exist = false;
        return state.Exist;
    }
    public IEnumerable<string>? GetCurrentIndexNames(string examineIndexName)
    {
        var state = service.GetIndexState(examineIndexName);
        return GetIndexesAssignedToAlias(GetClient(examineIndexName), state.IndexAlias);
    }
    public void EnsuredIndexExists(string examineIndexName, bool overrideExisting = false) => throw new NotImplementedException();
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
    public void CreateIndex(string examineIndexName)
    {
        var state = service.GetIndexState(examineIndexName);
        if (state.CreatingNewIndex)
        {
            return;
        }

        state.CreatingNewIndex = true;
        var indexName = CreateIndexName(state.IndexAlias);
        if (state.Reindexing)
        {
            state.CurrentTemporaryIndexName = indexName;
        }else
        {
            state.CurrentIndexName = indexName;
        }
        //assigned current indexName

    }
    public Properties? GetProperties(string examineIndexName)
    {
        var client = GetClient(examineIndexName);
        var state = service.GetIndexState(examineIndexName);

        var indexesMappedToAlias = GetIndexesAssignedToAlias(client, state.IndexAlias).ToList();
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
    public void SwapTempIndex(string? examineIndexName)
    {
        var client = GetClient(examineIndexName);
        var state = service.GetIndexState(examineIndexName);

        var bulkAliasResponse = client.Indices.UpdateAliases(x => x.Actions(a => a.Add(add => add.Index(state.CurrentTemporaryIndexName).Alias(state.TempIndexAliast))));
        state.CurrentIndexName = state.CurrentTemporaryIndexName;
        state.CurrentTemporaryIndexName = null;
        state.Reindexing = false;
    }
    public long IndexBatch(string? examineIndexName, IEnumerable<ValueSet> values) => throw new NotImplementedException();
    public long DeleteBatch(string? examineIndexName, IEnumerable<string> itemIds) => throw new NotImplementedException();
    public int GetDocumentCount(string? examineIndexName)
    {
        var client = GetClient(examineIndexName);
        var state = service.GetIndexState(examineIndexName);

      return  (int)client.Count(index => index.Index(state.CurrentIndexName)).Count;
    }
    public bool HealthCheck(string? examineIndexNam) => throw new NotImplementedException();
    private static string CreateIndexName(string indexAlias)
    {
        return $"{indexAlias}_{DateTime.UtcNow:yyyyMMddHHmmss}";
    }
}
