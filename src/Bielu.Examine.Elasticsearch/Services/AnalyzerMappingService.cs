using Bielu.Examine.Elasticsearch.Model;
using Elastic.Clients.Elasticsearch.Analysis;
using Elastic.Clients.Elasticsearch.Mapping;
using Examine;
using Microsoft.Extensions.Logging;

namespace Bielu.Examine.Elasticsearch.Services;

public class AnalyzerMappingService(IEnumerable<IAnalyzerProvider> analyzerProviders, ILogger<AnalyzerMappingService> logger) : IAnalyzerMappingService
{
    public AnalyzersDescriptor GetElasticSearchAnalyzerMapping(AnalyzersDescriptor aa, string analyzer)
    {
        var analyzerProvider = analyzerProviders.FirstOrDefault(x => x.IsAnalyzer(analyzer));
        if(analyzerProvider == null)
        {
#pragma warning disable CA1848
            logger.LogWarning("No analyzer provider found for {Analyzer}", analyzer);
#pragma warning restore CA1848
            return aa.Simple("standard");
        }
        return analyzerProvider.GetAnalyzerMapping(aa, analyzer);
    }
}
