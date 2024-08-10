using Elastic.Clients.Elasticsearch.Analysis;

namespace Bielu.Examine.Elasticsearch.Services;

public class PatternAnalyzer : IAnalyzerProvider
{
    public string AnalyzerName => "pattern";

    public bool IsAnalyzer(string analyzer)
    {
        return analyzer == AnalyzerName;
    }

    public AnalyzersDescriptor GetAnalyzerMapping(AnalyzersDescriptor aa, string analyzer)
    {
        return aa.Pattern(AnalyzerName);
    }
}
