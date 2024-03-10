using Bielu.Examine.Elasticsearch.Model;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Examine;
using Lucene.Net.Documents;

namespace Bielu.Examine.Elasticsearch.Services;

public interface IElasticsearchService
{
    public bool IndexExists(string examineIndexName);
    public IEnumerable<string>? GetCurrentIndexNames(string examineIndexName);
    public void EnsuredIndexExists(string examineIndexName, Func<PropertiesDescriptor<ElasticDocument>, PropertiesDescriptor<ElasticDocument>> fieldsMapping, bool overrideExisting = false);
    public void CreateIndex(string examineIndexName,Func<PropertiesDescriptor<ElasticDocument>, PropertiesDescriptor<ElasticDocument>> fieldsMapping);
    Properties? GetProperties(string examineIndexName);
    ElasticSearchSearchResults Search(string examineIndexName,SearchRequestDescriptor<ElasticDocument> searchDescriptor);
    ElasticSearchSearchResults Search(string examineIndexName, SearchRequest<Document> searchDescriptor);
    void SwapTempIndex(string? examineIndexName);
    long IndexBatch(string? examineIndexName, IEnumerable<ValueSet> values);
    long DeleteBatch(string? examineIndexName, IEnumerable<string> itemIds);
    int GetDocumentCount(string? examineIndexName);
    bool HealthCheck(string? examineIndexNam);
}
