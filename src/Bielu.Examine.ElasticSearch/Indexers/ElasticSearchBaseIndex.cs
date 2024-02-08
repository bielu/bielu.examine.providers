using System.Configuration;
using Bielu.Examine.ElasticSearch.Configuration;
using Bielu.Examine.ElasticSearch.Helpers;
using Bielu.Examine.ElasticSearch.Model;
using Bielu.Examine.ElasticSearch.Providers;
using Bielu.Examine.ElasticSearch.Services;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Examine;
using Examine.Lucene;
using Examine.Lucene.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IndexOptions = Examine.IndexOptions;

namespace Bielu.Examine.ElasticSearch.Indexers;

public class ElasticSearchBaseIndex(string name, ILoggerFactory loggerFactory, IElasticSearchClientFactory factory,IOptionsMonitor<IndexOptions> indexOptions, IOptionsMonitor<ExamineElasticOptions> examineElasticOptions) : BaseIndexProvider(loggerFactory, name, indexOptions), IDisposable
{
    private bool? _exists;
    private bool isReindexing = false;
    private static readonly object ExistsLocker = new object();
    private ElasticsearchClient _client
    {
        get
        {
            return factory.GetOrCreateClient(name);
        }
    }

    /// <summary>
    /// Occurs when [document writing].
    /// </summary>
    public event EventHandler<Events.DocumentWritingEventArgs> DocumentWriting;

    public string indexName { get; set; }
    private IndexConfiguration? _indexConfiguration { get; set; } = examineElasticOptions.CurrentValue.IndexConfigurations.FirstOrDefault(x => x.Name == name.ToLowerInvariant()) ?? new IndexConfiguration() {
        Name = name.ToLowerInvariant()
    };
    private string _prefix
    {
        get
        {
            return _indexConfiguration.Prefix;
        }
    }


    public string indexAlias { get; set; }
    private string tempindexAlias { get; set; }
    public string Analyzer { get; }


    protected virtual void FromExamineType(PropertiesDescriptor<ElasticDocument> descriptor, FieldDefinition field)
    {

        switch (field.Type.ToLowerInvariant())
        {
            case "date":
            case "datetimeoffset":
            case "datetime":
                descriptor.Date(s => field.Name);
                break;
            case "double":
                descriptor.DoubleNumber(s => field.Name);
                break;
            case "float":
                descriptor.FloatNumber(s => field.Name);
                break;

            case "long":
                descriptor.LongNumber(s => field.Name);
                break;
            case "int":
            case "number":
                descriptor.IntegerNumber(s => field.Name);
                break;
            case "raw":
                descriptor.Keyword(s => field.Name);
                break;
            default:
                descriptor.Text(s => field.Name, configure => configure.Analyzer(FromLuceneAnalyzer(Analyzer)));
                break;
        }
    }

    protected virtual void OnDocumentWriting(Events.DocumentWritingEventArgs docArgs)
    {
        DocumentWriting?.Invoke(this, docArgs);
    }

    private static string FromLuceneAnalyzer(string analyzer)
    {
        if (string.IsNullOrEmpty(analyzer) || !analyzer.Contains(","))
            return "simple";

        //if it contains a comma, we'll assume it's an assembly typed name


        if (analyzer.Contains("StandardAnalyzer"))
            return "standard";
        if (analyzer.Contains("WhitespaceAnalyzer"))
            return "whitespace";
        if (analyzer.Contains("SimpleAnalyzer"))
            return "simple";
        if (analyzer.Contains("KeywordAnalyzer"))
            return "keyword";
        if (analyzer.Contains("StopAnalyzer"))
            return "stop";
        if (analyzer.Contains("ArabicAnalyzer"))
            return "arabic";

        if (analyzer.Contains("BrazilianAnalyzer"))
            return "brazilian";

        if (analyzer.Contains("ChineseAnalyzer"))
            return "chinese";

        if (analyzer.Contains("CJKAnalyzer"))
            return "cjk";

        if (analyzer.Contains("CzechAnalyzer"))
            return "czech";

        if (analyzer.Contains("DutchAnalyzer"))
            return "dutch";

        if (analyzer.Contains("FrenchAnalyzer"))
            return "french";

        if (analyzer.Contains("GermanAnalyzer"))
            return "german";

        if (analyzer.Contains("RussianAnalyzer"))
            return "russian";
        if (analyzer.Contains("StopAnalyzer"))
            return "stop";
        //if the above fails, return standard
        return "simple";
    }

    public void EnsureIndex(bool forceOverwrite)
    {
        if (!forceOverwrite && _exists.HasValue && _exists.Value) return;

        var indexExists = IndexExists();
        if (indexExists && !forceOverwrite) return;
        if (TempIndexExists() && !isReindexing) return;
        CreateNewIndex(indexExists);
    }

    private void CreateNewIndex(bool indexExists)
    {
        lock (ExistsLocker)
        {
            _client.Indices.Delete((Indices)GetIndexAssignedToTempAlias().ToArray());
            var index = _client.Indices.Create(indexName, c => c
                .Mappings(ms => ms.Dynamic(DynamicMapping.Runtime)
                    .Properties(descriptor => descriptor.Date())
                )
            );
            var aliasExists = _client.Indices.Exists(indexAlias).Exists;


            var indexesMappedToAlias = aliasExists
                ? GetIndexAssignedToAlias().ToList()
                : new List<String>();
            if (!indexExists || (aliasExists && indexesMappedToAlias?.Count == 0))
            {
                var bulkAliasResponse =  _client.Indices.UpdateAliases(x=>x.Actions(a=>a.Add(add=>add.Index(indexName).Alias(indexAlias))));
            }
            else
            {
                isReindexing = true;
                var bulkAliasResponse =  _client.Indices.UpdateAliases(x=>x.Actions(a=>a.Add(add=>add.Index(indexName).Alias(tempindexAlias))));
            }

            _exists = true;
        }
    }

    private ElasticsearchExamineSearcher CreateSearcher()
    {
        return new ElasticsearchExamineSearcher(Name, indexName, LoggerFactory, _examineElasticOptions);
    }

    private ElasticsearchClient GetIndexClient()
    {
        return _factory.GetOrCreateClient(indexName);
    }

    public static string FormatFieldName(string fieldName)
    {
        return $"{fieldName.Replace(".", "_")}";
    }

    private BulkRequestDescriptor ToElasticSearchDocs(IEnumerable<ValueSet> docs, string indexTarget)
    {
        var descriptor = new BulkRequestDescriptor();


        foreach (var d in docs)
        {
            try
            {
                var indexingNodeDataArgs = new IndexingItemEventArgs(this, d);
                OnTransformingIndexValues(indexingNodeDataArgs);

                if (!indexingNodeDataArgs.Cancel)
                {
                    //this is just a dictionary
                    var ad = new ElasticDocument {
                        ["Id"] = d.Id,
                        [FormatFieldName(ExamineFieldNames.ItemIdFieldName)] = d.Id,
                        [FormatFieldName(ExamineFieldNames.ItemTypeFieldName)] = d.ItemType,
                        [FormatFieldName(ExamineFieldNames.CategoryFieldName)] = d.Category
                    };

                    foreach (var i in d.Values)
                    {
                        if (i.Value.Count > 0)
                            ad[FormatFieldName(i.Key)] = i.Value.Count == 1 ? i.Value[0] : i.Value;
                    }

                    var docArgs = new Events.DocumentWritingEventArgs(d, ad);
                    OnDocumentWriting(docArgs);
                    descriptor.Index<ElasticDocument>(ad, indexingNodeDataArgs => indexingNodeDataArgs.Index<ElasticDocument>());
                }
            }
            catch (Exception e)
            {
            }
        }

        return descriptor;
    }
    private IList<string> GetIndexAssignedToAlias()
    {
        return _client.GetIndexesAssignedToAlias(indexAlias);
    }
    private IList<string> GetIndexAssignedToTempAlias()
    {
        return _client.GetIndexesAssignedToAlias(tempindexAlias);
    }
    protected override void PerformIndexItems(IEnumerable<ValueSet> op, Action<IndexOperationEventArgs> onComplete)
    {
        var aliasExists = _client.IndexExists(indexAlias);
        var indexesMappedToAlias = aliasExists
            ? GetIndexAssignedToAlias()
            : new List<String>();
        EnsureIndex(false);

        var indexTarget = isReindexing ? tempindexAlias : indexAlias;
        var indexer = GetIndexClient();
        var totalResults = 0;
        var batch = ToElasticSearchDocs(op, indexTarget);
        var indexResult = indexer.Bulk(batch);
        totalResults += indexResult.Items.Count;


        if (isReindexing)
        {
            indexer.Indices.UpdateAliases(ba => ba
                .Actions(remove => remove.Remove(removeAction => removeAction.Alias(indexAlias))
                    .Add(add => add.Index(indexName).Alias(indexAlias))));


            indexesMappedToAlias.Where(e => e != indexName).ToList()
                .ForEach(e => _client.Indices.Delete(new DeleteIndexRequest(e)));
        }


        onComplete(new IndexOperationEventArgs(this, totalResults));
    }

    protected override void PerformDeleteFromIndex(IEnumerable<string> itemIds,
        Action<IndexOperationEventArgs> onComplete)
    {
     
    }


    public override void CreateIndex()
    {
        EnsureIndex(true);
    }

    public override bool IndexExists()
    {
        return _client.Indices.Exists(indexAlias).Exists;
    }

    public override ISearcher Searcher => _searcher.Value;

    public bool TempIndexExists()
    {
        return _client.Indices.Exists(tempindexAlias).Exists;
    }

    public void Dispose()
    {
    }


    public IEnumerable<string> GetFields()
    {
        return _searcher.Value.AllFields;
    }

    #region IIndexDiagnostics

    public int DocumentCount =>
        (int)(IndexExists() ? _client.Value.Count<ElasticDocument>(e => e.Indices(indexAlias)).Count : 0);

    public int FieldCount => IndexExists() ? _searcher.Value.AllFields.Length : 0;

    #endregion
}