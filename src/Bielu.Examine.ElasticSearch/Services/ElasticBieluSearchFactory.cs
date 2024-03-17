using System.Collections.Concurrent;
using Bielu.Examine.Core.Services;
using Bielu.Examine.Elasticsearch.Providers;
using Microsoft.Extensions.Logging;

namespace Bielu.Examine.Elasticsearch.Services;

public class ElasticBieluSearchManager(IIndexStateService stateService, ILoggerFactory loggerFactory, ISearchService service) : IBieluSearchManager
{
    private ConcurrentDictionary<string, IBieluExamineSearcher> _searchers = new ConcurrentDictionary<string, IBieluExamineSearcher>();
    public IBieluExamineSearcher GetSearcher(string? indexName)
    {
        ArgumentNullException.ThrowIfNull(indexName);
        if (_searchers.TryGetValue(indexName, out var searcher))
        {
            return searcher;
        }
        var state = stateService.GetIndexState(indexName);
        searcher = new ElasticsearchExamineSearcher(indexName, state.IndexAlias, loggerFactory, service);
        _searchers.TryAdd(indexName, searcher);
        return searcher;
    }
}
