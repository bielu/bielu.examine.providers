using Elastic.Clients.Elasticsearch.Analysis;

namespace Bielu.Examine.Elasticsearch.Services;

public class StopAnalyzerProvider : IAnalyzerProvider
{
    public string AnalyzerName => "stop";

    public bool IsAnalyzer(string analyzer)
    {
        return analyzer == AnalyzerName;
    }

    public AnalyzersDescriptor GetAnalyzerMapping(AnalyzersDescriptor aa, string analyzer)
    {
        return aa.Stop(AnalyzerName);
    }
}
