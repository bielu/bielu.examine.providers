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
using Examine.Lucene;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IndexOptions = Examine.IndexOptions;

namespace Bielu.Examine.Elasticsearch.Indexers;

public class ElasticSearchBaseIndex(string? name, ILogger<ElasticSearchBaseIndex> logger, ILoggerFactory loggerFactory, IElasticsearchService elasticSearchService, IIndexStateService indexStateService, IOptionsMonitor<LuceneDirectoryIndexOptions> indexOptions, IOptionsMonitor<BieluExamineElasticOptions> examineElasticOptions) : BaseIndexProvider(loggerFactory, name, indexOptions), IElasticSearchExamineIndex, IDisposable
{
    private bool? _exists;
    private ExamineIndexState IndexState => indexStateService.GetIndexState(name);
    private static readonly object _existsLocker = new object();
    public string? ElasticUrl { get; set; }
    public string? ElasticId => examineElasticOptions.CurrentValue.IndexConfigurations.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.AuthenticationDetails?.Id ?? examineElasticOptions.CurrentValue.DefaultIndexConfiguration.AuthenticationDetails?.Id;

    /// <summary>
    /// Occurs when [document writing].
    /// </summary>
    public event EventHandler<Events.DocumentWritingEventArgs> DocumentWriting;

    public string? IndexName => IndexState.IndexName;
    private IndexConfiguration? IndexConfiguration => examineElasticOptions.CurrentValue.IndexConfigurations.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? new IndexConfiguration()
    {
        Name = name.ToLowerInvariant()
    };


    public string? IndexAlias  => IndexState.IndexAlias;
    private string? TempindexAlias  => IndexState.TempIndexAliast;
    public string? Analyzer { get; }


    protected virtual void FromExamineType(ref PropertiesDescriptor<ElasticDocument> descriptor, FieldDefinition field)
    {
        var fieldType = field.Type.ToLowerInvariant();

        descriptor = fieldType switch
        {
            var type when _dateFormats.Contains(type) => descriptor.Date(s => field.Name),
            "double" => descriptor.DoubleNumber(s => field.Name),
            "float" => descriptor.FloatNumber(s => field.Name),
            "long" => descriptor.LongNumber(s => field.Name),
            var type when _integerFormats.Contains(type) => descriptor.IntegerNumber(s => field.Name),
            "raw" => descriptor.Keyword(s => field.Name),
            _ => descriptor.Text(s => field.Name, configure => configure.Analyzer(FromLuceneAnalyzer(Analyzer)))
        };
    }

    protected virtual void OnDocumentWriting(Events.DocumentWritingEventArgs docArgs)
    {
        DocumentWriting?.Invoke(this, docArgs);
    }

    private static string FromLuceneAnalyzer(string? analyzer)
    {
        return analyzer switch
        {
            null or "" => "simple",
            _ when !analyzer.Contains(',') => "simple",
            _ when analyzer.Contains("StandardAnalyzer") => "standard",
            _ when analyzer.Contains("WhitespaceAnalyzer") => "whitespace",
            _ when analyzer.Contains("SimpleAnalyzer") => "simple",
            _ when analyzer.Contains("KeywordAnalyzer") => "keyword",
            _ when analyzer.Contains("StopAnalyzer") => "stop",
            _ when analyzer.Contains("ArabicAnalyzer") => "arabic",
            _ when analyzer.Contains("BrazilianAnalyzer") => "brazilian",
            _ when analyzer.Contains("ChineseAnalyzer") => "chinese",
            _ when analyzer.Contains("CJKAnalyzer") => "cjk",
            _ when analyzer.Contains("CzechAnalyzer") => "czech",
            _ when analyzer.Contains("DutchAnalyzer") => "dutch",
            _ when analyzer.Contains("FrenchAnalyzer") => "french",
            _ when analyzer.Contains("GermanAnalyzer") => "german",
            _ when analyzer.Contains("RussianAnalyzer") => "russian",
            _ => "simple"
        };
    }



    private void CreateNewIndex(bool indexExists)
    {
        elasticSearchService.CreateIndex(name);
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
            FromExamineType(ref descriptor, field);
        }

        return descriptor;
    }
    private ElasticsearchExamineSearcher CreateSearcher()
    {
        return new ElasticsearchExamineSearcher(Name, IndexAlias, LoggerFactory, elasticSearchService);
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

    protected override void PerformIndexItems(IEnumerable<ValueSet> values, Action<IndexOperationEventArgs> onComplete)
    {
       long totalResults =elasticSearchService.IndexBatch(name,values);

        onComplete(new IndexOperationEventArgs(this, (int)totalResults));
    }

    protected override void PerformDeleteFromIndex(IEnumerable<string> itemIds,
        Action<IndexOperationEventArgs> onComplete)
    {
        long totalResults =elasticSearchService.DeleteBatch(name,itemIds);

        onComplete(new IndexOperationEventArgs(this, (int)totalResults));
    }


    public override void CreateIndex()
    {
        elasticSearchService.EnsuredIndexExists(name,true);
    }

    public override bool IndexExists()
    {
        if (_exists.HasValue)
        {
            return _exists.Value;
        }
        if (elasticSearchService.IndexExists(IndexName))
        {
            _exists = true;
        }
        else
        {
            _exists = false;
        }
        return _exists.Value;
    }

    public override ISearcher Searcher => CreateSearcher();
    public void SwapIndex()
    {
        elasticSearchService.SwapTempIndex(name);
    }

    public IEnumerable<string> GetFields() => ((ElasticsearchExamineSearcher)Searcher).AllFields;

    #region IIndexDiagnostics

    public int DocumentCount =>
        (int)(IndexExists() ? elasticSearchService.GetDocumentCount(name) : 0);

    public int FieldCount => IndexExists() ? GetFields().Count() : 0;

    private static readonly string[] _dateFormats = new[]
    {
        "date", "datetimeoffset", "datetime"
    };
    private static readonly string[] _integerFormats = new[]
    {
        "int", "number"
    };

    #endregion
#pragma warning disable CA1816
    public void Dispose()
 #pragma warning restore CA1816
    {

    }
}
