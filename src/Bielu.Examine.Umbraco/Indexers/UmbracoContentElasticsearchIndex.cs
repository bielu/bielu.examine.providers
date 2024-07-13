using Bielu.Examine.Core.Services;
using Examine;
using Examine.Lucene;
using Examine.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;

namespace bielu.Examine.Umbraco.Indexers.Indexers;

public class BieluExamineUmbracoContentIndex : BieluExamineUmbracoIndex, IUmbracoContentIndex

{
    private readonly ISet<string> _idOnlyFieldSet = new HashSet<string> { "id" };
    public BieluExamineUmbracoContentIndex(string? name, ILoggerFactory loggerFactory, IRuntime runtime, ILogger<IBieluExamineIndex> logger, ISearchService searchService, IIndexStateService stateService, IBieluSearchManager manager, IOptionsMonitor<LuceneDirectoryIndexOptions> indexOptions) : base(name, loggerFactory, runtime, logger, searchService, stateService, manager, indexOptions)
    {
        LuceneDirectoryIndexOptions namedOptions = indexOptions.Get(name);
        if (namedOptions == null)
        {
            throw new InvalidOperationException(
                $"No named {typeof(LuceneDirectoryIndexOptions)} options with name {name}");
        }

        if (namedOptions.Validator is IContentValueSetValidator contentValueSetValidator)
        {
            PublishedValuesOnly = contentValueSetValidator.PublishedValuesOnly;
            SupportProtectedContent = contentValueSetValidator.SupportProtectedContent;
        }
    }

    protected override void PerformIndexItems(IEnumerable<ValueSet> values, Action<IndexOperationEventArgs> onComplete)
    {
        // We don't want to re-enumerate this list, but we need to split it into 2x enumerables: invalid and valid items.
        // The Invalid items will be deleted, these are items that have invalid paths (i.e. moved to the recycle bin, etc...)
        // Then we'll index the Value group all together.
        var invalidOrValid = values.GroupBy(v =>
        {
            if (!v.Values.TryGetValue("path", out IReadOnlyList<object>? paths) || paths.Count <= 0 || paths[0] == null)
            {
                return ValueSetValidationStatus.Failed;
            }

            ValueSetValidationResult validationResult = ValueSetValidator.Validate(v);

            return validationResult.Status;
        }).ToArray();

        var hasDeletes = false;
        var hasUpdates = false;

        // ordering by descending so that Filtered/Failed processes first
        foreach (IGrouping<ValueSetValidationStatus, ValueSet> group in invalidOrValid.OrderByDescending(x => x.Key))
        {
            switch (group.Key)
            {
                case ValueSetValidationStatus.Valid:
                    hasUpdates = true;

                    //these are the valid ones, so just index them all at once
                    base.PerformIndexItems(group.ToArray(), onComplete);
                    break;
                case ValueSetValidationStatus.Failed:
                    // don't index anything that is invalid
                    break;
                case ValueSetValidationStatus.Filtered:
                    hasDeletes = true;

                    // these are the invalid/filtered items so we'll delete them
                    // since the path is not valid we need to delete this item in
                    // case it exists in the index already and has now
                    // been moved to an invalid parent.
                    base.PerformDeleteFromIndex(group.Select(x => x.Id), null);
                    break;
            }
        }

        if ((hasDeletes && !hasUpdates) || (!hasDeletes && !hasUpdates))
        {
            //we need to manually call the completed method
            onComplete(new IndexOperationEventArgs(this, 0));
        }
    }

    protected override void PerformDeleteFromIndex(IEnumerable<string> itemIds, Action<IndexOperationEventArgs> onComplete)
    {
        var idsAsList = itemIds.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var childIdsToDelete = new List<string>();

        for (var i = 0; i < idsAsList.Count; i++)
        {
            var nodeId = idsAsList[i];

            //find all descendants based on path
            var descendantPath = $@"\-1*\,{nodeId}\,*";
            var rawQuery = $"{UmbracoExamineFieldNames.IndexPathFieldName}:{descendantPath}";
            IQuery? c = Searcher.CreateQuery();
            IBooleanOperation? filtered = c.NativeQuery(rawQuery);
            IOrdering? selectedFields = filtered.SelectFields(_idOnlyFieldSet);
            ISearchResults? results = selectedFields.Execute();

            childIdsToDelete.AddRange(results.Select(x => x.Id));
            idsAsList.RemoveAll(x => childIdsToDelete.Contains(x));
        }

        idsAsList.AddRange(childIdsToDelete);

        var response = base.DeleteBatch(idsAsList.Distinct());
    }

}

