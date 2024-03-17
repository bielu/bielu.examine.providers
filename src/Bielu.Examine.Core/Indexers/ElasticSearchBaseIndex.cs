using Bielu.Examine.Core.Extensions;
using Bielu.Examine.Core.Models;
using Bielu.Examine.Core.Services;
using Examine;
using Examine.Lucene;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bielu.Examine.Core.Indexers;

public class ElasticSearchBaseIndex(string? name, ILogger<IBieluExamineIndex> logger, ILoggerFactory loggerFactory, ISearchService elasticSearchService, IIndexStateService indexStateService, IBieluSearchManager bieluSearchManager, IOptionsMonitor<LuceneDirectoryIndexOptions> indexOptions) : BaseIndexProvider(loggerFactory, name, indexOptions), IBieluExamineIndex, IDisposable
{
    private bool? _exists;
    private ExamineIndexState IndexState => indexStateService.GetIndexState(name);
    private static readonly object _existsLocker = new object();
    /// <summary>
    /// Occurs when [document writing].
    /// </summary>
    public event EventHandler<DocumentWritingEventArgs> DocumentWriting;

    public string? IndexName => IndexState.IndexName;

    public string? IndexAlias => IndexState.IndexAlias;
    private string? TempindexAlias => IndexState.TempIndexAlias;
    public string? Analyzer { get; }


    protected virtual void OnDocumentWriting(DocumentWritingEventArgs docArgs)
    {
        DocumentWriting?.Invoke(this, docArgs);
    }


    private IBieluExamineSearcher CreateSearcher() => bieluSearchManager.GetSearcher(name);


    protected override void PerformIndexItems(IEnumerable<ValueSet> values, Action<IndexOperationEventArgs> onComplete)
    {
        long totalResults = elasticSearchService.IndexBatch(name, values);

        onComplete(new IndexOperationEventArgs(this, (int)totalResults));
    }

    protected override void PerformDeleteFromIndex(IEnumerable<string> itemIds,
        Action<IndexOperationEventArgs> onComplete)
    {
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
}
