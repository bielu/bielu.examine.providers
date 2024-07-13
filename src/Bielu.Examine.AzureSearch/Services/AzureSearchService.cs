using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Bielu.Examine.Core.Extensions;
using Bielu.Examine.Core.Models;
using Bielu.Examine.Core.Queries;
using Bielu.Examine.Core.Regex;
using Bielu.Examine.Core.Services;
using Bielu.Examine.Elasticsearch.Events;
using Bielu.Examine.Elasticsearch.Extensions;
using Bielu.Examine.Elasticsearch.Model;
using Bielu.Examine.Elasticsearch.Services;
using Examine;
using Examine.Lucene.Search;
using Examine.Search;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;
using Query = Lucene.Net.Search.Query;

namespace Bielu.Examine.AzureSearch.Services;

public class AzureSearchService(IAzureSearchClientFactory factory, IIndexStateService service, IPropertyMappingService propertyMappingService, ILogger<AzureSearchService> logger, ILoggerFactory loggerFactory) : ISearchService
{
    readonly List<IObserver<TransformingObservable>> _observers = new();
    public bool IndexExists(string examineIndexName)
    {
        var client = GetIndexingClient(examineIndexName);
        var state = service.GetIndexState(examineIndexName,this);
        var aliasExist = client.GetAliases();

        if (AliasExist(state.IndexAlias))
        {
            state.Exist = true;
            state.CurrentIndexName ??= state.IndexAlias;
            return state.Exist;
        }
        state.Exist = false;
        return state.Exist;
    }
    public IEnumerable<string>? GetCurrentIndexNames(string examineIndexName)
    {
        var state = service.GetIndexState(examineIndexName,this);
        return GetIndexesAssignedToAlias(GetIndexingClient(examineIndexName), state.IndexAlias);
    }
    public void CreateIndex(string examineIndexName, string analyzer,  ReadOnlyFieldDefinitionCollection properties)
    {
        var fieldsMapping = propertyMappingService.GetAzureSearchMapping(properties, analyzer);
        CreateIndex(examineIndexName, fieldsMapping);
    }
    public BieluExamineSearchResults Search(string examineIndexName, QueryOptions? options, Query query)
    {
        var state = service.GetIndexState(examineIndexName, this);
        var searchOptions=new SearchOptions()
        {
            Filter = QueryRegex.PathRegex().Replace(query.ToString(), "$1\\-"),
            Skip = options?.Skip ?? 0,
            Size = options?.Take ?? 1000
        };

        return this.Search(examineIndexName, searchOptions);
    }
    public BieluExamineSearchResults Search(string examineIndexName, object searchDescriptor)
    {
        return searchDescriptor switch
        {
            SearchOptions descriptor => Search(examineIndexName, descriptor),
            _ => throw new InvalidOperationException("Invalid search descriptor")
        };
    }
    private AzureSearchSearchResults Search(string examineIndexName, SearchOptions searchDescriptor)
    {
        var client = GetSearchClient(examineIndexName);
        SearchResults<ElasticDocument>
            searchResult = client.Search<ElasticDocument>(searchDescriptor);
        return searchResult.ConvertToSearchResults();
    }
    public void EnsuredIndexExists(string examineIndexName, string analyzer, ReadOnlyFieldDefinitionCollection properties, bool overrideExisting = false)
    {
        var fieldsMapping = propertyMappingService.GetAzureSearchMapping(properties, analyzer);
        if (IndexExists(examineIndexName))
        {
            if (overrideExisting)
            {
                var state = service.GetIndexState(examineIndexName, this);
                state.Reindexing = true;
                CreateIndex(examineIndexName, fieldsMapping);
            }
        }
        else
        {
            var state = service.GetIndexState(examineIndexName, this);
            state.Reindexing = true;
            CreateIndex(examineIndexName, fieldsMapping);
        }
    }
    private List<string>? GetIndexesAssignedToAlias(SearchIndexClient client, string? aliasName)
    {
        ArgumentNullException.ThrowIfNull(aliasName);

        if (!IndexExists(aliasName))
        {
            return new List<string>();
        }
        var aliasExists = client.GetAlias(aliasName);
        if (aliasExists.HasValue && aliasExists.Value.Indexes.Count > 0)
        {
            return aliasExists.Value.Indexes.Select(x => x).ToList();
        }
        return new List<string>();
    }
    private SearchClient GetSearchClient(string examineIndexName)
    {
        var state = service.GetIndexState(examineIndexName);
        return factory.GetOrCreateSearchClient(state.IndexName);
    }

    private SearchIndexClient GetIndexingClient(string examineIndexName)
    {
        var state = service.GetIndexState(examineIndexName);
        return factory.GetOrCreateIndexClient(state.IndexName);
    }

    public void CreateIndex(string examineIndexName, IEnumerable<SearchFieldTemplate> fieldMapping)
    {
        var client = GetIndexingClient(examineIndexName);
        var state = service.GetIndexState(examineIndexName, this);
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
        var aliasExists = AliasExist(state.IndexAlias);
        var aliasIndexes = GetIndexesAssignedToAlias(client, state.IndexAlias);
        var indexesMappedToAlias = aliasExists
            ? GetIndexesAssignedToAlias(client, state.IndexAlias).ToList()
            : new List<String>();
        if (state.Reindexing)
        {
            client.CreateIndex( new SearchIndex(indexName,fieldMapping.Select(x=>(SearchField)x).ToList()));
            return;
        }
        if (!aliasExists  || !aliasIndexes?.Contains(state.IndexName) == true)
        {
            var createAlias =  client.CreateOrUpdateAlias(state.IndexAlias, new SearchAlias(state.IndexAlias, state.CurrentIndexName));
        }

    }

    private bool AliasExist(string alias)
    {
        var client = GetIndexingClient(alias);
        var aliasExist = client.GetAliases();
        if(aliasExist.All(x=>x.Name !=alias))
        {
            return false;
        }
        var aliasExists = client.GetAlias(alias).Value;
        var state = service.GetIndexState(alias, this);
        if (aliasExists != null && aliasExists.Indexes.Count > 0)
        {
            state.CurrentIndexName ??= aliasExists.Indexes[0];
            return state.Exist;
        }

        return false;
    }

    public IEnumerable<string>? GetPropertiesNames(string examineIndexName)
    {
        var client = GetIndexingClient(examineIndexName);

        var indexesMappedToAlias = GetIndexesAssignedToAlias(client, examineIndexName).ToList();
        if (indexesMappedToAlias.Count <= 0)
        {
            return null;
        }
        var index = client.GetIndex(indexesMappedToAlias.FirstOrDefault()).Value;
        return index.Fields.Select(x => x.Name).ToList();
    }
    public IEnumerable<ExamineProperty>? GetProperties(string examineIndexName)
    {
        var client = GetIndexingClient(examineIndexName);
        var state = service.GetIndexState(examineIndexName, this);

        var indexesMappedToAlias = GetIndexesAssignedToAlias(client, state.IndexAlias).ToList();
        if (indexesMappedToAlias.Count <= 0)
        {
            return null;
        }
        var index = client.GetIndex(indexesMappedToAlias.FirstOrDefault()).Value;

        return index.Fields.Select(x => new ExamineProperty()
        {
            Key = x.Name, Type = x.Type.ToString()
        }).ToList();
    }

    public void SwapTempIndex(string? examineIndexName)
    {
        var client = GetIndexingClient(examineIndexName);
        var state = service.GetIndexState(examineIndexName, this);
        var oldIndexes = GetIndexesAssignedToAlias(client, state.IndexAlias);
        var bulkAliasResponse = client.CreateOrUpdateAlias(state.IndexAlias, new SearchAlias(state.IndexAlias, state.CurrentTemporaryIndexName));
        state.CurrentIndexName = state.CurrentTemporaryIndexName;
        state.CurrentTemporaryIndexName = null;
        state.Reindexing = false;
        state.CreatingNewIndex = false;
        foreach (var index in oldIndexes)
        {
            client.DeleteIndex(index);
        }
    }
    public long IndexBatch(string? examineIndexName, IEnumerable<ValueSet> values)
    {
        var totalResults = 0L;
        var client = GetSearchClient(examineIndexName);
        var state = service.GetIndexState(examineIndexName, this);
        if (!IndexExists(examineIndexName))
        {
            return 0;
        }
        if (!values.Any())
        {
            return 0;
        }
        var batch = ToElasticSearchDocs(values);
        var indexResult = client.UploadDocuments(batch);
        totalResults += indexResult.Value.Results.Count;
        return totalResults;
    }
    public long DeleteBatch(string? examineIndexName, IEnumerable<string> itemIds)
    {
        var client = GetSearchClient(examineIndexName);
        var state = service.GetIndexState(examineIndexName, this);
        var delete= client.DeleteDocuments("Id", itemIds);

        return delete.Value.Results.Count;
    }
    public int GetDocumentCount(string? examineIndexName)
    {
        var state = service.GetIndexState(examineIndexName, this);
        var client = GetSearchClient(state.IndexAlias);
        if (!IndexExists(state.IndexAlias))
        {
            return 0;
        }
        return (int)client.GetDocumentCount();
    }
    public bool HealthCheck(string? examineIndexName)
    {
        var client = GetIndexingClient(examineIndexName);
        var state = service.GetIndexState(examineIndexName, this);
        if(!IndexExists(examineIndexName))
        {
            return false;
        }
        var index=client.GetIndex(state.IndexAlias);
        return index.HasValue && index.Value.Fields.Any();
    }
    public IQuery CreateQuery(string name, string? indexAlias, string category, BooleanOperation defaultOperation) => new BieluExamineQuery(name, indexAlias, new ElasticSearchQueryParser(LuceneVersion.LUCENE_CURRENT, GetPropertiesNames(name).ToArray(), new StandardAnalyzer(LuceneVersion.LUCENE_48)), this, loggerFactory, loggerFactory.CreateLogger<BieluExamineQuery>(), category, new LuceneSearchOptions(), defaultOperation);
    private static string CreateIndexName(string indexAlias)
    {
        return $"{indexAlias}_{DateTime.UtcNow:yyyyMMddHHmmss}";
    }
    private List<ElasticDocument> ToElasticSearchDocs(IEnumerable<ValueSet> docs)
    {
        var descriptor = new List<ElasticDocument>();


        foreach (var d in docs)
        {
            try
            {
                //var indexingNodeDataArgs = new IndexingItemEventArgs(this, d);
                //    OnTransformingIndexValues(indexingNodeDataArgs);
                var observable = new TransformingObservable() { ValueSet = d };
                foreach (var observer in _observers)
                {
                    observer.OnNext(observable);
                }

                if (observable.Cancel)
                {
                    continue;
                }
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

                    var docArgs = new DocumentWritingEventArgs(d, ad);
                    // OnDocumentWriting(docArgs);
                    descriptor.Add(ad);
                }
            }
            catch (Exception e)
            {
 #pragma warning disable CA1848
                logger.LogError(e, "Failed to index document {NodeID}", d.Id);
                foreach (var observer in _observers)
                {
                    observer.OnError(e);
                }
 #pragma warning restore CA1848
            }
            foreach (var observer in _observers)
            {
                observer.OnCompleted();
            }

            _observers.Clear();
        }

        return descriptor;
    }
    public IDisposable Subscribe(IObserver<TransformingObservable> observer)
    {
        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
        }

        return new Unsubscriber<TransformingObservable>(_observers, observer);
    }

}
