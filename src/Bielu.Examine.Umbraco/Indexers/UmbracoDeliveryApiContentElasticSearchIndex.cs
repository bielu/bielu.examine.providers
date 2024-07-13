using System.Globalization;
using Bielu.Examine.Core.Services;
using Examine;
using Examine.Lucene;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Extensions;

namespace bielu.Examine.Umbraco.Indexers.Indexers;

public class BieluExamineUmbracoDeliveryApiContentIndex : BieluExamineUmbracoIndex
{
    private readonly IDeliveryApiCompositeIdHandler _deliveryApiCompositeIdHandler;
    private readonly ILogger _logger;
    public BieluExamineUmbracoDeliveryApiContentIndex(string? name, ILoggerFactory loggerFactory, IRuntime runtime, ILogger<IBieluExamineIndex> logger, ISearchService searchService, IIndexStateService stateService, IBieluSearchManager manager, IOptionsMonitor<LuceneDirectoryIndexOptions> indexOptions, IDeliveryApiCompositeIdHandler deliveryApiCompositeIdHandler) : base(name, loggerFactory, runtime, logger, searchService, stateService, manager, indexOptions)
    {
        _logger = logger;
        _deliveryApiCompositeIdHandler = deliveryApiCompositeIdHandler;
        PublishedValuesOnly = false;
        EnableDefaultEventHandler = false;
    }

    protected override void PerformDeleteFromIndex(IEnumerable<string> itemIds, Action<IndexOperationEventArgs>? onComplete)
    {
        var removedIndexIds = new List<string>();
        var removedContentIds = new List<string>();
        foreach (var itemId in itemIds)
        {
            // if this item was already removed as a descendant of a previously removed item, skip it
            if (removedIndexIds.Contains(itemId))
            {
                continue;
            }

            // an item ID passed to this method can be a composite of content ID and culture (like "1234|da-DK") or simply a content ID
            // - when it's a composite ID, only the supplied culture of the given item should be deleted from the index
            // - when it's an content ID, all cultures of the of the given item should be deleted from the index
            var (contentId, culture) = ParseItemId(itemId);
            if (contentId is null)
            {
                _logger.LogWarning("Could not parse item ID; expected integer or composite ID, got: {itemId}", itemId);
                continue;
            }

            // if this item was already removed as a descendant of a previously removed item (for all cultures), skip it
            if (culture is null && removedContentIds.Contains(contentId))
            {
                continue;
            }

            // find descendants-or-self based on path and optional culture
            var rawQuery = $"({UmbracoExamineFieldNames.DeliveryApiContentIndex.Id}:{contentId} OR {UmbracoExamineFieldNames.IndexPathFieldName}:\\-1*,{contentId},*)";
            if (culture is not null)
            {
                rawQuery = $"{rawQuery} AND culture:{culture}";
            }

            ISearchResults results = Searcher
                .CreateQuery()
                .NativeQuery(rawQuery)
                // NOTE: we need to be explicit about fetching ItemIdFieldName here, otherwise Examine will try to be
                // clever and use the "id" field of the document (which we can't use for deletion)
                .SelectField(UmbracoExamineFieldNames.ItemIdFieldName)
                .Execute();

            _logger.LogDebug("DeleteFromIndex with query: {Query} (found {TotalItems} results)", rawQuery, results.TotalItemCount);

            // grab the index IDs from the index (the composite IDs)
            var indexIds = results.Select(x => x.Id).ToList();

            // remember which items we removed, so we can skip those later
            removedIndexIds.AddRange(indexIds);
            if (culture is null)
            {
                removedContentIds.AddRange(indexIds.Select(indexId => ParseItemId(indexId).ContentId).WhereNotNull());
            }

            // delete the resulting items from the index
            base.PerformDeleteFromIndex(indexIds, null);
        }
    }

    private (string? ContentId, string? Culture) ParseItemId(string id)
    {
        if (int.TryParse(id, out _))
        {
            return (id, null);
        }

        DeliveryApiIndexCompositeIdModel compositeIdModel = _deliveryApiCompositeIdHandler.Decompose(id);

        return (compositeIdModel.Id?.ToString(CultureInfo.InvariantCulture), compositeIdModel.Culture);
    }


    protected override void OnTransformingIndexValues(IndexingItemEventArgs e)
    {
        // UmbracoExamineIndex (base class down the hierarchy) performs some magic transformations here for paths and icons;
        // we don't want that for the Delivery API, so we'll have to override this method and simply do nothing.
    }
}
