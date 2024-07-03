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
    : BaseIndexProvider(loggerFactory, name, indexOptions), IBieluExamineIndex, IDisposable, IObserver<TransformingObservable>
{
    private bool _exists;
    private ExamineIndexState IndexState => indexStateService.GetIndexState(name);
    private static readonly object _existsLocker = new object();

    private IDisposable Unsubscriber;


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

    void IIndex.IndexItems(IEnumerable<ValueSet> values) => PerformIndexItems(values, OnIndexOperationComplete);

    protected override void PerformIndexItems(IEnumerable<ValueSet> values, Action<IndexOperationEventArgs> onComplete)
    {
        this.Subscribe(elasticSearchService);
        long totalResults = elasticSearchService.IndexBatch(name, values);
        this.Unsubscribe();
        onComplete?.Invoke(new IndexOperationEventArgs(this, (int)totalResults));
    }

    protected override void PerformDeleteFromIndex(IEnumerable<string> itemIds,
        Action<IndexOperationEventArgs> onComplete)
    {
        this.Subscribe(elasticSearchService);
        long totalResults = elasticSearchService.DeleteBatch(name, itemIds);
        this.Unsubscribe();
        onComplete?.Invoke(new IndexOperationEventArgs(this, (int)totalResults));
    }


    public override void CreateIndex()
    {
        _exists = false;
        elasticSearchService.EnsuredIndexExists(name, Analyzer, FieldDefinitions, true);
    }

    public override bool IndexExists()
    {
        if (!_exists)
        {
            _exists = elasticSearchService.IndexExists(IndexName);
        }

        return _exists;
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
        Unsubscriber.Dispose();
    }
    public virtual void Subscribe(IObservable<TransformingObservable> provider)
    {
        Unsubscriber = provider.Subscribe(this);
    }

    public virtual void Unsubscribe()
    {
        Unsubscriber.Dispose();
    }

    public void OnCompleted()
    {

    }

    public void OnError(Exception error)
    {

    }

    public void OnNext(TransformingObservable value)
    {
        var indexingNodeDataArgs = new IndexingItemEventArgs(this, value.ValueSet);
        OnTransformingIndexValues(indexingNodeDataArgs);
        //todo: test
        value.ValueSet = indexingNodeDataArgs.ValueSet;
        value.Cancel = indexingNodeDataArgs.Cancel;
    }
}
