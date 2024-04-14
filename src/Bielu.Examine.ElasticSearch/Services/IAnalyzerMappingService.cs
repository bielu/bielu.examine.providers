using Elastic.Clients.Elasticsearch.Analysis;

namespace Bielu.Examine.Elasticsearch.Services;

public interface IAnalyzerMappingService
{
    AnalyzersDescriptor GetElasticSearchAnalyzerMapping(AnalyzersDescriptor aa, string analyzer);
}
