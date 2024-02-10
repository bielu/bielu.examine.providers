using Bielu.Examine.ElasticSearch.Configuration;
using Bielu.Examine.ElasticSearch.Indexers;
using Bielu.Examine.ElasticSearch.Model;
using Bielu.Examine.ElasticSearch.Services;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Examine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Extensions;
using IndexOptions = Examine.IndexOptions;

namespace BIelu.Examine.Umbraco.Indexers
{
    public class ElasticSearchUmbracoIndex(string? name, ILoggerFactory loggerFactory, IElasticSearchClientFactory factory, IRuntime runtime, ILogger<ElasticSearchUmbracoIndex> logger, IOptionsMonitor<IndexOptions> indexOptions, IOptionsMonitor<BieluExamineElasticOptions> examineElasticOptions) : ElasticSearchBaseIndex(name, loggerFactory, factory, indexOptions, examineElasticOptions), IUmbracoIndex, IIndexDiagnostics
    {
        public const string SpecialFieldPrefix = "__";
        public const string IndexPathFieldName = SpecialFieldPrefix + "Path";
        public const string NodeKeyFieldName = SpecialFieldPrefix + "Key";
        public const string IconFieldName = SpecialFieldPrefix + "Icon";
        public const string PublishedFieldName = SpecialFieldPrefix + "Published";

        private readonly List<string> _keywordFields = new List<string>()
        {
            IndexPathFieldName
        };
        private readonly IProfilingLogger _logger;
        public bool EnableDefaultEventHandler { get; set; } = true;

        /// <summary>
        /// The prefix added to a field when it is duplicated in order to store the original raw value.
        /// </summary>
        public const string RawFieldPrefix = SpecialFieldPrefix + "Raw_";


        public long GetDocumentCount() => 0;
        public IEnumerable<string> GetFieldNames() => GetFields();
        public bool SupportProtectedContent { get; }
        private readonly bool _configBased;

        protected IProfilingLogger ProfilingLogger { get; }

        /// <summary>
        /// When set to true Umbraco will keep the index in sync with Umbraco data automatically
        /// </summary>

        public bool PublishedValuesOnly { get; internal set; }

        /// <summary>
        /// override to check if we can actually initialize.
        /// </summary>
        /// <remarks>
        /// This check is required since the base examine lib will try to rebuild on startup
        /// </remarks>
        /// <summary>
        /// Returns true if the Umbraco application is in a state that we can initialize the examine indexes
        /// </summary>
        /// <returns></returns>
        protected bool CanInitialize()
        {
            // only affects indexers that are config file based, if an index was created via code then
            // this has no effect, it is assumed the index would not be created if it could not be initialized
            return _configBased == false || runtime.State.Level == RuntimeLevel.Run;
        }

        /// <summary>
        /// overridden for logging
        /// </summary>
        /// <param name="e"></param>
        protected override void OnIndexingError(IndexingErrorEventArgs e)
        {
 #pragma warning disable CA1848
            logger.LogError(e.Exception, "Error indexing item {NodeId}", e.ItemId);
 #pragma warning restore CA1848
            base.OnIndexingError(e);
        }
        public override PropertiesDescriptor<ElasticDocument> CreateFieldsMapping(PropertiesDescriptor<ElasticDocument> descriptor,
            ReadOnlyFieldDefinitionCollection fieldDefinitionCollection)
        {
            descriptor.Keyword(s => FormatFieldName("Id"));
            descriptor.Keyword(s => FormatFieldName(ExamineFieldNames.ItemIdFieldName));
            descriptor.Keyword(s => FormatFieldName(ExamineFieldNames.ItemTypeFieldName));
            descriptor.Keyword(s => FormatFieldName(ExamineFieldNames.CategoryFieldName));
            foreach (FieldDefinition field in fieldDefinitionCollection)
            {
                FromExamineType(descriptor, field);
            }

            //  var docArgs = new MappingOperationEventArgs(descriptor);
            // onMapping(docArgs);

            return descriptor;
        }
        protected override void FromExamineType(PropertiesDescriptor<ElasticDocument> descriptor, FieldDefinition field)
        {

            if (_keywordFields.Contains(field.Name))
            {
                descriptor.Keyword(s => FormatFieldName(field.Name));
                return;
            }
            base.FromExamineType(descriptor, field);
        }
        protected override void PerformDeleteFromIndex(IEnumerable<string> itemIds,
            Action<IndexOperationEventArgs> onComplete)
        {

            var descriptor = new BulkRequestDescriptor();
            descriptor = descriptor;
            foreach (var id in itemIds.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                descriptor.Delete(x => x.Index(IndexName).Id(id));
            }

            var response = factory.GetOrCreateClient(IndexName).Bulk(descriptor);
            if (response.Errors)
            {
                foreach (var itemWithError in response.ItemsWithErrors)
                {
 #pragma warning disable CA1848
                    logger.LogError("Failed to remove from index document {NodeID}: {Error}",
                        itemWithError.Id, itemWithError.Error);
 #pragma warning restore CA1848
                }
            }
        }


        protected override void OnTransformingIndexValues(IndexingItemEventArgs e)
        {
            base.OnTransformingIndexValues(e);

            var updatedValues = e.ValueSet.Values.ToDictionary(x => x.Key, x => (IEnumerable<object>)x.Value);

            //ensure special __Path field
            var path = e.ValueSet.GetValue("path");
            if (path != null)
            {
                updatedValues[UmbracoExamineFieldNames.IndexPathFieldName] = path.Yield();
            }

            //icon
            if (e.ValueSet.Values.TryGetValue("icon", out IReadOnlyList<object>? icon) &&
                e.ValueSet.Values.ContainsKey(UmbracoExamineFieldNames.IconFieldName) == false)
            {
                updatedValues[UmbracoExamineFieldNames.IconFieldName] = icon;
            }

            e.SetValues(updatedValues);
        }


        public Attempt<string> IsHealthy()
        {
            var isHealthy = factory.GetOrCreateClient(IndexName).Cluster.Health();
            return isHealthy.Status ==  HealthStatus.Green ||  isHealthy.Status ==  HealthStatus.Yellow
                ? Attempt<string>.Succeed()
                : Attempt.Fail("ElasticSearch cluster is not healthy");
        }

        public IReadOnlyDictionary<string, object?> Metadata
        {
            get
            {
                var d = new Dictionary<string, object?>();
                d[nameof(DocumentCount)] = DocumentCount;
                d[nameof(Name)] = Name;
                d[nameof(IndexAlias)] = IndexAlias;
                d[nameof(IndexName)] = IndexName;
                d[nameof(ElasticUrl)] = ElasticUrl;
                d[nameof(ElasticId)] = ElasticId;
                d[nameof(Analyzer)] = Analyzer;
                d[nameof(EnableDefaultEventHandler)] = EnableDefaultEventHandler;
                d[nameof(PublishedValuesOnly)] = PublishedValuesOnly;

                if (ValueSetValidator is ValueSetValidator vsv)
                {
                    d[nameof(ContentValueSetValidator.IncludeItemTypes)] = vsv.IncludeItemTypes;
                    d[nameof(ContentValueSetValidator.ExcludeItemTypes)] = vsv.ExcludeItemTypes;
                    d[nameof(ContentValueSetValidator.IncludeFields)] = vsv.IncludeFields;
                    d[nameof(ContentValueSetValidator.ExcludeFields)] = vsv.ExcludeFields;
                }

                if (ValueSetValidator is ContentValueSetValidator cvsv)
                {
                    d[nameof(ContentValueSetValidator.PublishedValuesOnly)] = cvsv.PublishedValuesOnly;
                    d[nameof(ContentValueSetValidator.SupportProtectedContent)] = cvsv.SupportProtectedContent;
                    d[nameof(ContentValueSetValidator.ParentId)] = cvsv.ParentId;
                }

                d[nameof(FieldDefinitionCollection)] = String.Join(", ", Searcher);
                return d.Where(x => x.Value != null).ToDictionary(x => x.Key, x => x.Value);
            }
        }
    }
}
