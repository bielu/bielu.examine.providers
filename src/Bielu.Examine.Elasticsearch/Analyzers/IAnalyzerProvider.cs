using Elastic.Clients.Elasticsearch.Analysis;

namespace Bielu.Examine.Elasticsearch.Services;

public interface IAnalyzerProvider
{
    string AnalyzerName { get; }
    bool IsAnalyzer(string analyzer);
    AnalyzersDescriptor GetAnalyzerMapping(AnalyzersDescriptor aa, string analyzer);
}
