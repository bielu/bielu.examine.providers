using Bielu.Examine.Elasticsearch.Model;
using Bielu.Examine.Elasticsearch.Services;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Examine;
using Lucene.Net.Documents;
using Umbraco.Cms.Core.Sync;

namespace Bielu.Examine.Elasticsearch.Umbraco.Services;

public class UmbracoElasticSearchServiceDecorator(IElasticsearchService elasticsearchService, IServerRoleAccessor serverRoleAccessor) : IElasticsearchService
{

    public bool IndexExists(string examineIndexName) => elasticsearchService.IndexExists(examineIndexName);
    public IEnumerable<string>? GetCurrentIndexNames(string examineIndexName) => elasticsearchService.GetCurrentIndexNames(examineIndexName);
    public void EnsuredIndexExists(string examineIndexName, Func<PropertiesDescriptor<ElasticDocument>, PropertiesDescriptor<ElasticDocument>> fieldsMapping, bool overrideExisting = false)
    {
        if (serverRoleAccessor.CurrentServerRole == ServerRole.SchedulingPublisher || serverRoleAccessor.CurrentServerRole == ServerRole.Single)
        {
            elasticsearchService.EnsuredIndexExists(examineIndexName,fieldsMapping, overrideExisting);
        }
        else
        {
            if (!IndexExists(examineIndexName))
            {
                throw new InvalidOperationException("Index does not exist and server is a subscriber.");
            }
        }
    }
    public void CreateIndex(string examineIndexName, Func<PropertiesDescriptor<ElasticDocument>, PropertiesDescriptor<ElasticDocument>> fieldsMapping)
    {
        if (serverRoleAccessor.CurrentServerRole == ServerRole.SchedulingPublisher || serverRoleAccessor.CurrentServerRole == ServerRole.Single)
        {
            elasticsearchService.CreateIndex(examineIndexName,fieldsMapping);
        }
    }
    public Properties? GetProperties(string examineIndexName) => elasticsearchService.GetProperties(examineIndexName);
    public ElasticSearchSearchResults Search(string examineIndexName, SearchRequestDescriptor<ElasticDocument> searchDescriptor) => elasticsearchService.Search(examineIndexName, searchDescriptor);
    public ElasticSearchSearchResults Search(string examineIndexName, SearchRequest<Document> searchDescriptor) => elasticsearchService.Search(examineIndexName, searchDescriptor);
    public void SwapTempIndex(string? examineIndexName)
    {
        if (serverRoleAccessor.CurrentServerRole == ServerRole.SchedulingPublisher || serverRoleAccessor.CurrentServerRole == ServerRole.Single)
        {
            elasticsearchService.SwapTempIndex(examineIndexName);
        }
    }
    public long IndexBatch(string? examineIndexName, IEnumerable<ValueSet> values)
    {
        if (serverRoleAccessor.CurrentServerRole == ServerRole.SchedulingPublisher || serverRoleAccessor.CurrentServerRole == ServerRole.Single)
        {
            return elasticsearchService.IndexBatch(examineIndexName, values);
        }
        return 0;
    }
    public long DeleteBatch(string? examineIndexName, IEnumerable<string> itemIds)
    {
        if (serverRoleAccessor.CurrentServerRole == ServerRole.SchedulingPublisher || serverRoleAccessor.CurrentServerRole == ServerRole.Single)
        {
            return elasticsearchService.DeleteBatch(examineIndexName, itemIds);
        }
        return 0;
    }
    public int GetDocumentCount(string? examineIndexName) => elasticsearchService.GetDocumentCount(examineIndexName);
    public bool HealthCheck(string? examineIndexNam) => elasticsearchService.HealthCheck(examineIndexNam);
}
