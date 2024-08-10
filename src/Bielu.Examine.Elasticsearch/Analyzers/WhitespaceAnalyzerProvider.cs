using Elastic.Clients.Elasticsearch.Analysis;

namespace Bielu.Examine.Elasticsearch.Services;

public class WhitespaceAnalyzerProvider : IAnalyzerProvider
{
    public string AnalyzerName => "whitespace";

    public bool IsAnalyzer(string analyzer)
    {
        return analyzer == AnalyzerName;
    }

    public AnalyzersDescriptor GetAnalyzerMapping(AnalyzersDescriptor aa, string analyzer)
    {
        return aa.Whitespace(AnalyzerName);
    }
}
