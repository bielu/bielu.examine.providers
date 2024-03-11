using Bielu.Examine.Core.Extensions;
using Bielu.Examine.Elasticsearch.Extensions;
using Bielu.Examine.Elasticsearch.Helpers;
using Bielu.Examine.Elasticsearch.Model;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Transport.Extensions;
using Examine;
using Lucene.Net.Documents;
using Microsoft.Extensions.Logging;

namespace Bielu.Examine.Elasticsearch.Services;

public class ElasticsearchService(IElasticSearchClientFactory factory, IIndexStateService service, ILogger<ElasticsearchService> logger) : IElasticsearchService
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
                state.CurrentIndexName ??= indexesMappedToAlias.Keys.First().ToString();

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
    public void EnsuredIndexExists(string examineIndexName, Func<PropertiesDescriptor<ElasticDocument>, PropertiesDescriptor<ElasticDocument>> fieldsMapping, bool overrideExisting = false)
    {
        if (IndexExists(examineIndexName))
        {
            if (overrideExisting)
            {
                var state = service.GetIndexState(examineIndexName);
                state.Reindexing = true;
                CreateIndex(examineIndexName, fieldsMapping);
            }
        }
        else
        {
            var state = service.GetIndexState(examineIndexName);
            state.Reindexing = true;
            CreateIndex(examineIndexName, fieldsMapping);
        }
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
    public void CreateIndex(string examineIndexName, Func<PropertiesDescriptor<ElasticDocument>, PropertiesDescriptor<ElasticDocument>> fieldsMapping)
    {
        var client = GetClient(examineIndexName);
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
        }
        else
        {
            state.CurrentIndexName = indexName;
        }
        //assigned current indexName
        var index = client.Indices.Create(indexName, c => c
            .Mappings(ms => ms.Dynamic(DynamicMapping.Runtime)
                .Properties<ElasticDocument>(descriptor =>
                    fieldsMapping(descriptor)
                )
            )
        );
        var aliasExists = client.Indices.Exists(state.IndexAlias).Exists;

        var indexesMappedToAlias = aliasExists
            ? GetIndexesAssignedToAlias(client, indexName).ToList()
            : new List<String>();
        if (!aliasExists)
        {
            var createAlias = client.Indices.PutAlias(indexName, state.IndexAlias);
        }


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
    public ElasticSearchSearchResults Search(string examineIndexName, SearchRequestDescriptor<ElasticDocument> searchDescriptor)
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
        var oldIndexes = GetIndexesAssignedToAlias(client, state.IndexAlias);
        var bulkAliasResponse = client.Indices.UpdateAliases(x => x.Actions(a => a.Add(add => add.Index(state.CurrentTemporaryIndexName).Alias(state.IndexAlias))));
        state.CurrentIndexName = state.CurrentTemporaryIndexName;
        state.CurrentTemporaryIndexName = null;
        state.Reindexing = false;
        state.CreatingNewIndex = false;
        foreach (var index in oldIndexes)
        {
            if (state.CurrentIndexName == index)
            {
                continue;
            }
            client.Indices.Delete(index);
        }
        client.Indices.DeleteAlias(state.CurrentIndexName, state.TempIndexAlias);
    }
    public long IndexBatch(string? examineIndexName, IEnumerable<ValueSet> values)
    {
        var totalResults = 0L;
        var client = GetClient(examineIndexName);
        var state = service.GetIndexState(examineIndexName);
        if (!IndexExists(examineIndexName))
        {
            return 0;
        }
        if(!values.Any())
        {
            return 0;
        }
        var batch = ToElasticSearchDocs(values, state.Reindexing ? state.CurrentTemporaryIndexName : state.CurrentIndexName);
        var indexResult = client.Bulk(batch);
        totalResults += indexResult.Items.Count;
        return totalResults;
    }
    public long DeleteBatch(string? examineIndexName, IEnumerable<string> itemIds)
    {
        var client = GetClient(examineIndexName);
        var state = service.GetIndexState(examineIndexName);
        var descriptor = new BulkRequestDescriptor();
        foreach (var id in itemIds)
        {
            descriptor.Delete(id, index => index.Index(state.CurrentIndexName));
        }
        client.Bulk(descriptor);
        return itemIds.Count();
    }
    public int GetDocumentCount(string? examineIndexName)
    {
        var client = GetClient(examineIndexName);
        var state = service.GetIndexState(examineIndexName);

        return (int)client.Count(index => index.Index(state.CurrentIndexName)).Count;
    }
    public bool HealthCheck(string? examineIndexNam)
    {
        var client = GetClient(examineIndexNam);
        return client.Cluster.Health().Status == HealthStatus.Green || client.Cluster.Health().Status == HealthStatus.Yellow;

    }
    private static string CreateIndexName(string indexAlias)
    {
        return $"{indexAlias}_{DateTime.UtcNow:yyyyMMddHHmmss}";
    }
    private BulkRequestDescriptor ToElasticSearchDocs(IEnumerable<ValueSet> docs, string? indexTarget)
    {
        var descriptor = new BulkRequestDescriptor();


        foreach (var d in docs)
        {
            try
            {
                //var indexingNodeDataArgs = new IndexingItemEventArgs(this, d);
            //    OnTransformingIndexValues(indexingNodeDataArgs);

                if (true)
                {
                    //this is just a dictionary
                    var ad = new ElasticDocument
                    {
                        ["Id"] = d.Id,
                        [ExamineFieldNames.ItemIdFieldName.FormatFieldName()] = d.Id,
                        [ExamineFieldNames.ItemTypeFieldName.FormatFieldName()] = d.ItemType,
                        [ExamineFieldNames.CategoryFieldName.FormatFieldName()] = d.Category
                    };

                    foreach (var i in d.Values)
                    {
                        if (i.Value.Count > 0)
                            ad[i.Key.FormatFieldName()] = i.Value.Count == 1 ? i.Value[0] : i.Value;
                    }

                    var docArgs = new Events.DocumentWritingEventArgs(d, ad);
                   // OnDocumentWriting(docArgs);
                   descriptor=  descriptor.Index<ElasticDocument>(ad, indexTarget,indexingNodeDataArgs => indexingNodeDataArgs.Index(indexTarget).Id(ad["Id"].ToString()));
                }
            }
            catch (Exception e)
            {
 #pragma warning disable CA1848
                logger.LogError(e, "Failed to index document {NodeID}", d.Id);
 #pragma warning restore CA1848
            }
        }

        return descriptor;
    }
}
