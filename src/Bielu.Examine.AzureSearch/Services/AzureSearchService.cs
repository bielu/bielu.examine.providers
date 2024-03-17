using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Bielu.Examine.Core.Extensions;
using Bielu.Examine.Core.Models;
using Bielu.Examine.Core.Queries;
using Bielu.Examine.Core.Regex;
using Bielu.Examine.Core.Services;
using Bielu.Examine.Elasticsearch.Model;
using Bielu.Examine.Elasticsearch.Services;
using Examine;
using Examine.Lucene.Search;
using Examine.Search;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;
using Query = Lucene.Net.Search.Query;

namespace Bielu.Examine.AzureSearch.Services;

public class AzureSearchService(IAzureSearchClientFactory factory, IIndexStateService service, IPropertyMappingService propertyMappingService, ILogger<AzureSearchService> logger, ILoggerFactory loggerFactory) : ISearchService
{
    public bool IndexExists(string examineIndexName)
    {
        var state = service.GetIndexState(examineIndexName);
        var client = GetIndexingClient(examineIndexName);
        var aliasExists = client.GetAlias(state.IndexAlias).Value;
        if (aliasExists != null && aliasExists.Indexes.Count > 0)
        {
            state.Exist = true;
            state.CurrentIndexName ??= aliasExists.Indexes[0];
            return state.Exist;
        }
        state.Exist = false;
        return state.Exist;
    }
    public IEnumerable<string>? GetCurrentIndexNames(string examineIndexName)
    {
        var state = service.GetIndexState(examineIndexName);
        return GetIndexesAssignedToAlias(GetIndexingClient(examineIndexName), state.IndexAlias);
    }
    public void CreateIndex(string examineIndexName, string analyzer,  ReadOnlyFieldDefinitionCollection properties)
    {
        var fieldsMapping = propertyMappingService.GetAzureSearchMapping(properties, analyzer);
        CreateIndex(examineIndexName, fieldsMapping);
    }
    public BieluExamineSearchResults Search(string examineIndexName, QueryOptions? options, Query query)
    {
        var state = service.GetIndexState(examineIndexName);
        var searchOptions=new SearchOptions()
        {
            Filter = QueryRegex.PathRegex().Replace(query.ToString(), "$1\\-")
        };
        var searchResult = GetClient(examineIndexName).Search(searchOptions);
        SearchRequestDescriptor<ElasticDocument> searchDescriptor = new SearchRequestDescriptor<ElasticDocument>();
        searchDescriptor.Index(state.IndexAlias)
            .Size(options?.Take ?? 1000)
            .From(options?.Skip ?? 0)
            .Query(queryContainer);
        return this.Search(examineIndexName, searchDescriptor);
    }
    public BieluExamineSearchResults Search(string examineIndexName, object searchDescriptor) => throw new NotImplementedException();
    public BieluExamineSearchResults Search(string examineIndexName, object searchDescriptor, Query query) => throw new NotImplementedException();
    public void EnsuredIndexExists(string examineIndexName, string analyzer, ReadOnlyFieldDefinitionCollection properties, bool overrideExisting = false)
    {
        var fieldsMapping = propertyMappingService.GetAzureSearchMapping(properties, analyzer);
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
    private static List<string>? GetIndexesAssignedToAlias(SearchIndexClient client, string? aliasName)
    {
        ArgumentNullException.ThrowIfNull(aliasName);

        var aliasExists = client.GetAlias(aliasName);
        if (aliasExists.HasValue && aliasExists.Value.Indexes.Count > 0)
        {
            return aliasExists.Value.Indexes.Select(x => x).ToList();
        }
        return new List<string>();
    }
    private SearchClient GetSearchClient(string examineIndexName) => factory.GetOrCreateSearchClient(examineIndexName);
    private SearchIndexClient GetIndexingClient(string examineIndexName) => factory.GetOrCreateIndexClient(examineIndexName);
    public void CreateIndex(string examineIndexName, IEnumerable<SearchFieldTemplate> fieldMapping)
    {
        var client = GetIndexingClient(examineIndexName);
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
    public IEnumerable<string>? GetPropertiesNames(string examineIndexName)
    {
        var client = GetSearchClient(examineIndexName);
        var state = service.GetIndexState(examineIndexName);

        var indexesMappedToAlias = GetIndexesAssignedToAlias(client, state.IndexAlias).ToList();
        if (indexesMappedToAlias.Count <= 0)
        {
            return null;
        }
        GetMappingResponse response =
            client.Indices.GetMapping(mapping => mapping.Indices(indexesMappedToAlias[0]));
        var properties = response.GetMappingFor(indexesMappedToAlias[0]).Properties;
        return properties.Select(x => x.Key.Name).ToList();
    }
    public IEnumerable<ExamineProperty>? GetProperties(string examineIndexName)
    {
        var client = GetSearchClient(examineIndexName);
        var state = service.GetIndexState(examineIndexName);

        var indexesMappedToAlias = GetIndexesAssignedToAlias(client, state.IndexAlias).ToList();
        if (indexesMappedToAlias.Count <= 0)
        {
            return null;
        }
        GetMappingResponse response =
            client.Indices.GetMapping(mapping => mapping.Indices(indexesMappedToAlias[0]));
        var properties = response.GetMappingFor(indexesMappedToAlias[0]).Properties;
        return properties.Select(x => new ExamineProperty()
        {
            Key = x.Key.Name, Type = x.Value.Type.ToString()
        }).ToList();
    }
    public AzureSearchSearchResults Search(string examineIndexName, SearchRequestDescriptor<ElasticDocument> searchDescriptor)
    {
        var client = GetSearchClient(examineIndexName);
        SearchResponse<ElasticDocument>
            searchResult = client.Search<ElasticDocument>(searchDescriptor);
        return searchResult.ConvertToSearchResults();
    }
    public AzureSearchSearchResults Search(string examineIndexName, SearchRequest<Document> searchDescriptor)
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
        if (!values.Any())
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
    public IQuery CreateQuery(string name, string? indexAlias, string category, BooleanOperation defaultOperation) => new BieluExamineQuery(name, indexAlias, new ElasticSearchQueryParser(LuceneVersion.LUCENE_CURRENT, GetPropertiesNames(name).ToArray(), new StandardAnalyzer(LuceneVersion.LUCENE_48)), this, loggerFactory, loggerFactory.CreateLogger<BieluExamineQuery>(), category, new LuceneSearchOptions(), defaultOperation);
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

                    var docArgs = new Elasticsearch.Events.DocumentWritingEventArgs(d, ad);
                    // OnDocumentWriting(docArgs);
                    descriptor = descriptor.Index<ElasticDocument>(ad, indexTarget, indexingNodeDataArgs => indexingNodeDataArgs.Index(indexTarget).Id(ad["Id"].ToString()));
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
