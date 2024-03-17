using Bielu.Examine.Core.Extensions;
using Bielu.Examine.Core.Indexers;
using Bielu.Examine.Core.Services;
using Examine;
using Examine.Lucene;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Extensions;

namespace bielu.Examine.Umbraco.Indexers.Indexers
{
    public class ElasticSearchUmbracoIndex(string? name, ILoggerFactory loggerFactory, IRuntime runtime, ILogger<ElasticSearchUmbracoIndex> logger,ISearchService searchService, IIndexStateService stateService, IBieluSearchManager manager, IOptionsMonitor<LuceneDirectoryIndexOptions> indexOptions) : ElasticSearchBaseIndex(name, logger, loggerFactory, searchService,stateService,manager,indexOptions), IBieluExamineIndex, IIndexDiagnostics
    {

        public const string SpecialFieldPrefix = "__";
        public const string IndexPathFieldName = SpecialFieldPrefix + "Path";
        public const string NodeKeyFieldName = SpecialFieldPrefix + "Key";
        public const string IconFieldName = SpecialFieldPrefix + "Icon";
        public const string PublishedFieldName = SpecialFieldPrefix + "Published";

        private readonly IProfilingLogger _logger;
        public bool EnableDefaultEventHandler { get; set; } = true;
        public override string Name => name;
        /// <summary>
        /// The prefix added to a field when it is duplicated in order to store the original raw value.
        /// </summary>
        public const string RawFieldPrefix = SpecialFieldPrefix + "Raw_";


        public long GetDocumentCount() => 0;
        public IEnumerable<string> GetFieldNames() => GetFields();
        public bool SupportProtectedContent => CurrentContentValueSetValidator?.SupportProtectedContent ?? false;
        private readonly bool _configBased;

        protected IProfilingLogger ProfilingLogger { get; }

        /// <summary>
        /// When set to true Umbraco will keep the index in sync with Umbraco data automatically
        /// </summary>

        public bool PublishedValuesOnly => CurrentContentValueSetValidator?.PublishedValuesOnly ?? false;
        private IContentValueSetValidator? CurrentContentValueSetValidator => indexOptions.CurrentValue.Validator as IContentValueSetValidator;
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

        protected override void PerformDeleteFromIndex(IEnumerable<string> itemIds,
            Action<IndexOperationEventArgs> onComplete)
        {

            var response = searchService.DeleteBatch(name,itemIds.Where(x => !string.IsNullOrWhiteSpace(x)));
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


        public Attempt<string?> IsHealthy()
        {
            var isHealthy = searchService.HealthCheck(name);
            return isHealthy
                ? Attempt<string?>.Succeed()
                : Attempt.Fail("ElasticSearch cluster is not healthy");
        }

        public IReadOnlyDictionary<string, object?> Metadata
        {
            get
            {
                var metadata = new Dictionary<string, object?>();

                AddGeneralMetadata(metadata);
                AddValueSetValidatorMetadata(metadata);
                AddContentValueSetValidatorMetadata(metadata);
                AddFieldDefinitionCollectionMetadata(metadata);

                return metadata.Where(x => x.Value != null).ToDictionary(x => x.Key, x => x.Value);
            }
        }

        private void AddGeneralMetadata(Dictionary<string, object?> metadata)
        {
            var state = stateService.GetIndexState(name);
            metadata[nameof(DocumentCount)] = DocumentCount;
            metadata[nameof(Name)] = Name;
            metadata[nameof(IndexAlias)] = IndexAlias;
            metadata[nameof(FieldCount)] = GetFields().Count();
            metadata[nameof(IndexName)] = state.IndexName;
            metadata[nameof(state.CurrentIndexName)] = state.CurrentIndexName;
            metadata[nameof(state.Reindexing)] = state.Reindexing;
            metadata[nameof(state.CreatingNewIndex)] = state.CreatingNewIndex;
            metadata[nameof(IndexName)] = state.IndexName;
            metadata[nameof(Analyzer)] = Analyzer;
            metadata[nameof(EnableDefaultEventHandler)] = EnableDefaultEventHandler;
            metadata[nameof(PublishedValuesOnly)] = PublishedValuesOnly;
        }

        private void AddValueSetValidatorMetadata(Dictionary<string, object?> metadata)
        {
            if (ValueSetValidator is ValueSetValidator vsv)
            {
                metadata[nameof(ContentValueSetValidator.IncludeItemTypes)] = vsv.IncludeItemTypes;
                metadata[nameof(ContentValueSetValidator.ExcludeItemTypes)] = vsv.ExcludeItemTypes;
                metadata[nameof(ContentValueSetValidator.IncludeFields)] = vsv.IncludeFields;
                metadata[nameof(ContentValueSetValidator.ExcludeFields)] = vsv.ExcludeFields;
            }
        }

        private void AddContentValueSetValidatorMetadata(Dictionary<string, object?> metadata)
        {
            if (ValueSetValidator is ContentValueSetValidator cvsv)
            {
                metadata[nameof(ContentValueSetValidator.PublishedValuesOnly)] = cvsv.PublishedValuesOnly;
                metadata[nameof(ContentValueSetValidator.SupportProtectedContent)] = cvsv.SupportProtectedContent;
                metadata[nameof(ContentValueSetValidator.ParentId)] = cvsv.ParentId;
            }
        }

        private void AddFieldDefinitionCollectionMetadata(Dictionary<string, object?> metadata)
        {
            metadata[nameof(FieldDefinitionCollection)] = String.Join(", ", (Searcher as IBieluExamineSearcher).AllFields);
        }
    }
}
