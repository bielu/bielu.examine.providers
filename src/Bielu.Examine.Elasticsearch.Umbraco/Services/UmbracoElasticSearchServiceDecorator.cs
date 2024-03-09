using Bielu.Examine.Elasticsearch.Model;
using Bielu.Examine.Elasticsearch.Services;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Lucene.Net.Documents;
using Umbraco.Cms.Core.Sync;

namespace Bielu.Examine.Elasticsearch.Umbraco.Services;

public class UmbracoElasticSearchServiceDecorator(IElasticsearchService elasticsearchService, IServerRoleAccessor serverRoleAccessor) : IElasticsearchService
{

    public bool IndexExists(string examineIndexName) => elasticsearchService.IndexExists(examineIndexName);
    public IEnumerable<string>? GetCurrentIndexNames(string examineIndexName) => elasticsearchService.GetCurrentIndexNames(examineIndexName);
    public void EnsuredIndexExists(string examineIndexName)
    {
        if (serverRoleAccessor.CurrentServerRole == ServerRole.SchedulingPublisher || serverRoleAccessor.CurrentServerRole == ServerRole.Single)
        {
            elasticsearchService.EnsuredIndexExists(examineIndexName);
        }
        else
        {
            if(!IndexExists(examineIndexName))
            {
                throw new InvalidOperationException("Index does not exist and server is a subscriber.");
            }
        }
    }
    public void CreateIndex(string examineIndexName)
    {
        if (serverRoleAccessor.CurrentServerRole == ServerRole.SchedulingPublisher || serverRoleAccessor.CurrentServerRole == ServerRole.Single)
        {
            elasticsearchService.CreateIndex(examineIndexName);
        }
    }
    public Properties? GetProperties(string examineIndexName) => elasticsearchService.GetProperties(examineIndexName);
    public ElasticSearchSearchResults Search(string examineIndexName, SearchRequestDescriptor<ElasticDocument> searchDescriptor) => elasticsearchService.Search(examineIndexName, searchDescriptor);
    public ElasticSearchSearchResults Search(string examineIndexName, SearchRequest<Document> searchDescriptor) => elasticsearchService.Search(examineIndexName, searchDescriptor);
    public void SwapTempIndex(string? examineIndexName)   {
        if (serverRoleAccessor.CurrentServerRole == ServerRole.SchedulingPublisher || serverRoleAccessor.CurrentServerRole == ServerRole.Single)
        {
            elasticsearchService.SwapTempIndex(examineIndexName);
        }
    }
}
