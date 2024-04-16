using Bielu.Examine.Core.Constants;
using Bielu.Examine.Core.Extensions;
using Bielu.Examine.Core.Models;
using Bielu.Examine.Core.Services;
using Examine;
using Examine.Lucene;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bielu.Examine.Core.Indexers;

public class ElasticSearchBaseIndex(
    string? name,
    ILogger<IBieluExamineIndex> logger,
    ILoggerFactory loggerFactory,
    ISearchService elasticSearchService,
    IIndexStateService indexStateService,
    IBieluSearchManager bieluSearchManager,
    IOptionsMonitor<LuceneDirectoryIndexOptions> indexOptions)
    : BaseIndexProvider(loggerFactory, name, indexOptions), IBieluExamineIndex, IDisposable, IObserver<ValueSet>
{
    private bool? _exists;
    private ExamineIndexState IndexState => indexStateService.GetIndexState(name);
    private static readonly object _existsLocker = new object();

    /// <summary>
    /// Occurs when [document writing].
    /// </summary>
    public event EventHandler<DocumentWritingEventArgs> DocumentWriting;

    public event EventHandler<IndexingItemEventArgs> TransformingIndexValues;
    public string? IndexName => IndexState.IndexName;

    public string? IndexAlias => IndexState.IndexAlias;
    private string? TempindexAlias => IndexState.TempIndexAlias;
    public string? Analyzer => IndexState.Analyzer;


    protected virtual void OnDocumentWriting(DocumentWritingEventArgs docArgs)
    {
        DocumentWriting?.Invoke(this, docArgs);
    }


    private IBieluExamineSearcher CreateSearcher() => bieluSearchManager.GetSearcher(name);


    protected override void PerformIndexItems(IEnumerable<ValueSet> values, Action<IndexOperationEventArgs> onComplete)
    {
        List<ValueSet> listValues = [];

        long totalResults = elasticSearchService.IndexBatch(name, listValues);

        onComplete(new IndexOperationEventArgs(this, (int)totalResults));
    }

    protected override void PerformDeleteFromIndex(IEnumerable<string> itemIds,
        Action<IndexOperationEventArgs> onComplete)
    {
        foreach (var id in itemIds)
        {
            var indexingNodeDataArgs = new IndexingItemEventArgs(this, new ValueSet(id));
            OnTransformingIndexValues(indexingNodeDataArgs);
        }

        long totalResults = elasticSearchService.DeleteBatch(name, itemIds);

        onComplete(new IndexOperationEventArgs(this, (int)totalResults));
    }


    public override void CreateIndex()
    {
        elasticSearchService.EnsuredIndexExists(name, Analyzer, FieldDefinitions, true);
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

    public IEnumerable<string> GetFields() => ((IBieluExamineSearcher)Searcher).AllFields;

    protected virtual void OnTransformingIndexValues(IndexingItemEventArgs e) =>
        TransformingIndexValues?.Invoke(this, e);

    #region IIndexDiagnostics

    public int DocumentCount =>
        (int)(IndexExists() ? elasticSearchService.GetDocumentCount(name) : 0);

    public int FieldCount => IndexExists() ? GetFields().Count() : 0;

    #endregion

#pragma warning disable CA1816
    public void Dispose()
#pragma warning restore CA1816
    {
    }

    public void OnCompleted() => throw new NotImplementedException();

    public void OnError(Exception error) => throw new NotImplementedException();

    public void OnNext(ValueSet value)
    {
        var indexingNodeDataArgs = new IndexingItemEventArgs(this, value);
        OnTransformingIndexValues(indexingNodeDataArgs);
        if (indexingNodeDataArgs.Cancel)
        {
            //todo: test
            value = new ValueSet(BieluExamineConstants.CancelledValueSet);
        }
    }
}
