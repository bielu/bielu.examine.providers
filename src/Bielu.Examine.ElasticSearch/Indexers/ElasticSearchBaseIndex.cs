using System.Globalization;
using Bielu.Examine.Elasticsearch.Configuration;
using Bielu.Examine.Elasticsearch.Helpers;
using Bielu.Examine.Elasticsearch.Model;
using Bielu.Examine.Elasticsearch.Providers;
using Bielu.Examine.Elasticsearch.Services;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Examine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IndexOptions = Examine.IndexOptions;

namespace Bielu.Examine.Elasticsearch.Indexers;

public class ElasticSearchBaseIndex(string? name, ILogger<ElasticSearchBaseIndex> logger, ILoggerFactory loggerFactory, IElasticSearchClientFactory factory, IOptionsMonitor<IndexOptions> indexOptions, IOptionsMonitor<BieluExamineElasticOptions> examineElasticOptions) : BaseIndexProvider(loggerFactory, name, indexOptions),IElasticSearchExamineIndex, IDisposable
{
    private bool? _exists;
    public string? CurrentIndexName { get; set; } = string.Empty;
    private bool _isReindexing;
    private bool _isCreatingNewIndex;
    private static readonly object _existsLocker = new object();
    public string? ElasticUrl { get; set; }
    public  string? ElasticId => examineElasticOptions.CurrentValue.IndexConfigurations.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.AuthenticationDetails?.Id;
    public ElasticsearchClient Client => factory.GetOrCreateClient(name);

    /// <summary>
    /// Occurs when [document writing].
    /// </summary>
    public event EventHandler<Events.DocumentWritingEventArgs> DocumentWriting;

    public string? IndexName { get { return $"{Prefix}{name}"; } }
    private IndexConfiguration? IndexConfiguration => examineElasticOptions.CurrentValue.IndexConfigurations.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? new IndexConfiguration()
    {
        Name = name.ToLowerInvariant()
    };
    private string Prefix => IndexConfiguration.Prefix;


    public string? IndexAlias { get { return name.ToLowerInvariant(); } }
    private string? TempindexAlias { get { return name+"Temp".ToLowerInvariant(); } }
    public string? Analyzer { get; }


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

    private static string FromLuceneAnalyzer(string? analyzer)
    {
        if (string.IsNullOrEmpty(analyzer) || !analyzer.Contains(','))
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
        if (!forceOverwrite && _exists.HasValue && _exists.Value) {return;}

        var indexExists = IndexExists();
        if (indexExists && !forceOverwrite) return;
        if (TempIndexExists() && !_isReindexing) return;
        CreateNewIndex(indexExists);
    }
    private string _currentSuffix = string.Empty;
    private string PrepareIndexName()
    {
        if(_currentSuffix == string.Empty)
        {
            _currentSuffix = DateTime.Now.ToString("yyyyMMddHHmmss",CultureInfo.InvariantCulture);
        }
        return $"{IndexName}{_currentSuffix}".ToLowerInvariant();
    }
    private void CreateNewIndex(bool indexExists)
    {
        if (_isCreatingNewIndex)
        {
            return;
        }
        lock (_existsLocker)
        {
            _isCreatingNewIndex = true;
            var indexes = GetIndexAssignedToTempAlias().ToArray();
            if (indexes.Length != 0)
            {
                Client.Indices.Delete((Indices)indexes);

            }
            _currentSuffix = DateTime.Now.ToString("yyyyMMddHHmmss",CultureInfo.InvariantCulture);
            var currentIndexName = PrepareIndexName();
            var index = Client.Indices.Create(currentIndexName, c => c
                .Mappings(ms => ms.Dynamic(DynamicMapping.Runtime)
                    .Properties<ElasticDocument>(descriptor => CreateFieldsMapping(descriptor,FieldDefinitions ))
                )
            );
            var aliasExists = Client.Indices.Exists(IndexAlias).Exists;

            var indexesMappedToAlias = aliasExists
                ? GetIndexAssignedToAlias().ToList()
                : new List<String>();
            if (!aliasExists)
            {
                var createAlias = Client.Indices.PutAlias(currentIndexName, IndexAlias);

            }
           else if (!indexExists || ( indexesMappedToAlias?.Count == 0))
            {
                var bulkAliasResponse = Client.Indices.UpdateAliases(x => x.Actions(a => a.Add(add => add.Index(IndexName).Alias(IndexAlias))));

            }
            else
            {
                _isReindexing = true;
                var bulkAliasResponse = Client.Indices.UpdateAliases(x => x.Actions(a => a.Add(add => add.Index(IndexName).Alias(TempindexAlias))));
            }
            CleanOldIndexes();
            _isCreatingNewIndex = false;
            CurrentIndexName = currentIndexName;
            _exists = true;
        }
    }
    private static void CleanOldIndexes()
    {
        //todo: implement
    }
    public virtual PropertiesDescriptor<ElasticDocument> CreateFieldsMapping(PropertiesDescriptor<ElasticDocument> descriptor,
        ReadOnlyFieldDefinitionCollection fieldDefinitionCollection)
    {

        descriptor.Keyword(s => "Id");
        descriptor.Keyword(s => FormatFieldName(ExamineFieldNames.ItemIdFieldName));
        descriptor.Keyword(s => FormatFieldName(ExamineFieldNames.ItemTypeFieldName));
        descriptor.Keyword(s => FormatFieldName(ExamineFieldNames.CategoryFieldName));

        foreach (FieldDefinition field in fieldDefinitionCollection)
        {
            FromExamineType(descriptor, field);
        }

        return descriptor;
    }
    private ElasticsearchExamineSearcher CreateSearcher()
    {
        return new ElasticsearchExamineSearcher(Name, IndexAlias, LoggerFactory, factory, examineElasticOptions);
    }

    private ElasticsearchClient GetIndexClient()
    {
        return factory.GetOrCreateClient(IndexName);
    }

    public static string FormatFieldName(string fieldName)
    {
        return $"{fieldName.Replace(".", "_")}";
    }

    private BulkRequestDescriptor ToElasticSearchDocs(IEnumerable<ValueSet> docs, string? indexTarget)
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
                    var ad = new ElasticDocument
                    {
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
                    descriptor.Index<ElasticDocument>(ad, indexingNodeDataArgs => indexingNodeDataArgs.Index(indexTarget).Id(ad["Id"].ToString()));
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
    private IList<string> GetIndexAssignedToAlias()
    {
        return Client.GetIndexesAssignedToAlias(IndexAlias);
    }
    private IList<string> GetIndexAssignedToTempAlias()
    {
        return Client.GetIndexesAssignedToAlias(TempindexAlias);
    }
    protected override void PerformIndexItems(IEnumerable<ValueSet> values, Action<IndexOperationEventArgs> onComplete)
    {
        var aliasExists = Client.IndexExists(IndexAlias);
        var indexesMappedToAlias = aliasExists
            ? GetIndexAssignedToAlias()
            : new List<String>();
        EnsureIndex(false);

        var indexTarget = _isReindexing ? TempindexAlias : CurrentIndexName;
        var indexer = GetIndexClient();
        var totalResults = 0;
        var batch = ToElasticSearchDocs(values, indexTarget);
        var indexResult = indexer.Bulk(batch);
        totalResults += indexResult.Items.Count;


        if (_isReindexing)
        {
            indexer.Indices.UpdateAliases(ba => ba
                .Actions(remove => remove.Remove(removeAction => removeAction.Alias(IndexAlias))
                    .Add(add => add.Index(IndexName).Alias(IndexAlias))));


            indexesMappedToAlias.Where(e => e != IndexName).ToList()
                .ForEach(e => Client.Indices.Delete(new DeleteIndexRequest(e)));
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
        _exists = Client.IndexExists(IndexAlias);
        if(_exists.Value)
        {
            CurrentIndexName = Client.GetIndexesAssignedToAlias(IndexAlias).FirstOrDefault();
        }
        return _exists.Value;
    }

    public override ISearcher Searcher => CreateSearcher();

    public bool TempIndexExists()
    {
        return Client.Indices.Exists(TempindexAlias).Exists;
    }

    public IEnumerable<string> GetFields() => ((ElasticsearchExamineSearcher)Searcher).AllFields;

    #region IIndexDiagnostics

    public int DocumentCount =>
        (int)(IndexExists() ? Client.Count<ElasticDocument>(e => e.Indices(IndexAlias)).Count : 0);

    public int FieldCount => IndexExists() ?  GetFields().Count() : 0;

    #endregion
 #pragma warning disable CA1816
    public void Dispose()
 #pragma warning restore CA1816
    {

    }
}
