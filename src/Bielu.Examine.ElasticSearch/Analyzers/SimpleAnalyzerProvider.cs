using Elastic.Clients.Elasticsearch.Analysis;

namespace Bielu.Examine.Elasticsearch.Services;

public class SimpleAnalyzerProvider : IAnalyzerProvider
{
    public string AnalyzerName { get; } = "simple";
    public bool IsAnalyzer(string analyzer) => AnalyzerName.Equals(analyzer, StringComparison.OrdinalIgnoreCase);

    public AnalyzersDescriptor GetAnalyzerMapping(AnalyzersDescriptor aa, string analyzer) => aa.Simple(AnalyzerName);
}
