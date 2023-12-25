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
using Lucene.Net.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IndexOptions = Examine.IndexOptions;

namespace Bielu.Examine.ElasticSearch.Indexers;

public class ElasticSearchBaseIndex : BaseIndexProvider, IDisposable
{
    private readonly IOptionsMonitor<ExamineElasticOptions> _examineElasticOptions;
    private readonly IElasticSearchClientFactory _factory;
    private bool? _exists;
        private bool isReindexing = false;
        private bool _isUmbraco = false;
        public readonly Lazy<ElasticsearchClient> _client;
        private static readonly object ExistsLocker = new object();
        public readonly Lazy<ElasticsearchExamineSearcher> _searcher;

        /// <summary>
        /// Occurs when [document writing].
        /// </summary>
        public event EventHandler<DocumentWritingEventArgs> DocumentWriting;

        public string indexName { get; set; }

        private string prefix = ConfigurationManager.AppSettings.AllKeys.Any(s => s == "examine:ElasticSearch.Prefix")
            ? ConfigurationManager.AppSettings["examine:ElasticSearch.Prefix"]
            : "";

        public readonly string ElasticID;

        public string indexAlias { get; set; }
        private string tempindexAlias { get; set; }
        public string ElasticURL { get; set; }


       

   

        public string Analyzer { get; }

        public virtual PropertiesDescriptor<ElasticDocument> CreateFieldsMapping(PropertiesDescriptor<ElasticDocument> descriptor,
            FieldDefinitionCollection fieldDefinitionCollection)
        {
           

            return descriptor;
        }

        protected virtual void FromExamineType(PropertiesDescriptor<Document> descriptor, FieldDefinition field)
        {
         
            switch (field.Type.ToLowerInvariant())
            {
                case "date":
                case "datetimeoffset":
                case "datetime":
                    descriptor.Date(s =>field.Name);
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
                    descriptor.Text(s => field.Name, configure =>configure.Analyzer(FromLuceneAnalyzer(Analyzer)));
                    break;
            }
        }

        protected virtual void OnDocumentWriting(DocumentWritingEventArgs docArgs)
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
                _client.Value.Indices.bul(ba => ba
                    .Remove(remove => remove.Index("*").Alias(tempindexAlias)));
                indexName = prefix + Name + "_" +
                            DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
                var index = _client.Value.Indices.Create(indexName, c => c
                    .Mappings(ms => ms.Map<Document>(
                        m => m.AutoMap()
                            .Properties(ps => CreateFieldsMapping(ps, FieldDefinitionCollection))
                    ))
                );
                var aliasExists = _client.Value.Indices.Exists(indexAlias).Exists;


                var indexesMappedToAlias = aliasExists
                    ? _client.Value.GetIndicesPointingToAlias(indexAlias).ToList()
                    : new List<String>();
                if (!indexExists || (aliasExists && indexesMappedToAlias?.Count == 0))
                {
                    var bulkAliasResponse = _client.Value.Indices.BulkAlias(ba => ba
                        .Add(add => add.Index(indexName).Alias(indexAlias))
                    );
                }
                else
                {
                    isReindexing = true;
                    _client.Value.Indices.BulkAlias(ba => ba
                        .Add(add => add.Index(indexName).Alias(tempindexAlias))
                    );
                }

                _exists = true;
            }
        }

        private ElasticsearchExamineSearcher CreateSearcher()
        {
            return new ElasticsearchExamineSearcher(Name, indexName);
        }

        private ElasticsearchClient GetIndexClient()
        {
            return _factory.GetOrCreateClient(indexName);
        }

        public static string FormatFieldName(string fieldName)
        {
            return $"{fieldName.Replace(".", "_")}";
        }

        private BulkDescriptor ToElasticSearchDocs(IEnumerable<ValueSet> docs, string indexTarget)
        {
            var descriptor = new BulkDescriptor();


            foreach (var d in docs)
            {
                try
                {
                    var indexingNodeDataArgs = new IndexingItemEventArgs(this, d);
                    OnTransformingIndexValues(indexingNodeDataArgs);
                    
                    if (!indexingNodeDataArgs.Cancel) {
                        //this is just a dictionary
                        var ad = new Document
                        {
                            ["Id"] = d.Id,
                            [FormatFieldName(LuceneIndex.ItemIdFieldName)] = d.Id,
                            [FormatFieldName(LuceneIndex.ItemTypeFieldName)] = d.ItemType,
                            [FormatFieldName(LuceneIndex.CategoryFieldName)] = d.Category
                        };

                        foreach (var i in d.Values)
                        {
                            if (i.Value.Count > 0)
                                ad[FormatFieldName(i.Key)] = i.Value.Count == 1 ? i.Value[0] : i.Value;
                        }

                        var docArgs = new DocumentWritingEventArgs(d, ad);
                        OnDocumentWriting(docArgs);
                        descriptor.Index<Document>(op => op.Index(indexTarget).Document(ad).Id(d.Id));
                    }
                }
                catch (Exception e)
                {
                }
            }

            return descriptor;
        }

        protected override void PerformIndexItems(IEnumerable<ValueSet> op, Action<IndexOperationEventArgs> onComplete)
        {
            var aliasExists =  _client.Value.IndexExists(indexAlias);
            var indexesMappedToAlias = aliasExists
                ? _client.Value.GetIndexesAssignedToAlias(indexAlias).ToList()
                : new List<String>();
            EnsureIndex(false);

            var indexTarget = isReindexing ? tempindexAlias : indexAlias;
            var indexer = GetIndexClient();
            var totalResults = 0;
            var batch = ToElasticSearchDocs(op, indexTarget);
            var indexResult = indexer.Bulk(e => batch);
            totalResults += indexResult.Items.Count;


            if (isReindexing)
            {
                indexer.Indices.UpdateAliases(ba => ba
                    .Actions(remove => remove.Remove(removeAction => removeAction.Alias(indexAlias))
                    .Add(add => add.Index(indexName).Alias(indexAlias))));


                indexesMappedToAlias.Where(e => e != indexName).ToList()
                    .ForEach(e => _client.Value.Indices.Delete(new DeleteIndexRequest(e)));
            }


            onComplete(new IndexOperationEventArgs(this, totalResults));
        }

        protected override void PerformDeleteFromIndex(IEnumerable<string> itemIds,
            Action<IndexOperationEventArgs> onComplete)
        {
            var descriptor = new deel();

            foreach (var id in itemIds.Where(x => !string.IsNullOrWhiteSpace(x)))
                descriptor.Index(indexAlias).Delete<Document>(x => x
                        .Id(id))
                    .Refresh(Refresh.WaitFor);

            var response = _client.Value.Bulk(descriptor);
        }

     

        public override void CreateIndex()
        {
            EnsureIndex(true);
        }

        public override bool IndexExists()
        {
           return _client.Value.Indices.Exists(indexAlias).Exists;
        }

        public override ISearcher Searcher => _searcher.Value;

        public bool TempIndexExists()
        {
           return _client.Value.Indices.Exists(tempindexAlias).Exists;
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
            (int) (IndexExists() ? _client.Value.Count<ElasticDocument>(e => e.Indices(indexAlias)).Count : 0);

        public int FieldCount => IndexExists() ? _searcher.Value.AllFields.Length : 0;

        #endregion

        public ElasticSearchBaseIndex(ILoggerFactory loggerFactory, string name, IOptionsMonitor<IndexOptions> indexOptions, IOptionsMonitor<ExamineElasticOptions> examineElasticOptions, IElasticSearchClientFactory factory) : base(loggerFactory, name, indexOptions)
        {
            _examineElasticOptions = examineElasticOptions;
            _factory = factory;
        }
}
}