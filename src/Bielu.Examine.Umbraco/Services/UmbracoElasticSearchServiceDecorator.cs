using Bielu.Examine.Core.Models;
using Bielu.Examine.Core.Services;
using Examine;
using Examine.Lucene.Search;
using Examine.Search;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Umbraco.Cms.Core.Sync;

namespace Bielu.Examine.Elasticsearch.Umbraco.Services;

public class UmbracoSearchServiceDecorator(ISearchService searchService, IServerRoleAccessor serverRoleAccessor) : ISearchService
{

    public bool IndexExists(string examineIndexName) => searchService.IndexExists(examineIndexName);
    public IEnumerable<string>? GetCurrentIndexNames(string examineIndexName) => searchService.GetCurrentIndexNames(examineIndexName);
    public void EnsuredIndexExists(string examineIndexName, string analyzer, ReadOnlyFieldDefinitionCollection properties, bool overrideExisting = false)
    {
        if (serverRoleAccessor.CurrentServerRole == ServerRole.SchedulingPublisher || serverRoleAccessor.CurrentServerRole == ServerRole.Single)
        {
            searchService.EnsuredIndexExists(examineIndexName, analyzer, properties, overrideExisting);
        }
    }
    public void CreateIndex(string examineIndexName, string analyzer, ReadOnlyFieldDefinitionCollection properties)
    {
        if (serverRoleAccessor.CurrentServerRole == ServerRole.SchedulingPublisher || serverRoleAccessor.CurrentServerRole == ServerRole.Single)
        {
            searchService.CreateIndex(examineIndexName, analyzer,properties);
        }
    }
    public IEnumerable<ExamineProperty>? GetProperties(string examineIndexName) => searchService.GetProperties(examineIndexName);
    public BieluExamineSearchResults Search(string examineIndexName, QueryOptions? options, Query query) => searchService.Search(examineIndexName, options, query);
    public BieluExamineSearchResults Search(string examineIndexName, object searchDescriptor) => searchService.Search(examineIndexName, searchDescriptor);
    public void SwapTempIndex(string? examineIndexName) => searchService.SwapTempIndex(examineIndexName);
    public long IndexBatch(string? examineIndexName, IEnumerable<ValueSet> values)
    {
        if (serverRoleAccessor.CurrentServerRole == ServerRole.SchedulingPublisher || serverRoleAccessor.CurrentServerRole == ServerRole.Single)
        {
            return searchService.IndexBatch(examineIndexName, values);
        }
        return 0;
    }
    public long DeleteBatch(string? examineIndexName, IEnumerable<string> itemIds)
    {
        if (serverRoleAccessor.CurrentServerRole == ServerRole.SchedulingPublisher || serverRoleAccessor.CurrentServerRole == ServerRole.Single)
        {
            return searchService.DeleteBatch(examineIndexName, itemIds);
        }
        return 0;
    }
    public int GetDocumentCount(string? examineIndexName) => searchService.GetDocumentCount(examineIndexName);
    public IQuery CreateQuery(string name, string? indexAlias, string category, BooleanOperation defaultOperation, Analyzer? luceneAnalyzer, LuceneSearchOptions? searchOptions) => searchService.CreateQuery(name, indexAlias, category, defaultOperation, luceneAnalyzer, searchOptions);
    public bool HealthCheck(string? examineIndexName) => searchService.HealthCheck(examineIndexName);
    public IDisposable Subscribe(IObserver<TransformingObservable> observer) => searchService.Subscribe(observer);
}
