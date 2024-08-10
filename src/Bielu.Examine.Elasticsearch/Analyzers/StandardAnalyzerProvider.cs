using Elastic.Clients.Elasticsearch.Analysis;

namespace Bielu.Examine.Elasticsearch.Services;

public class StandardAnalyzerProvider : IAnalyzerProvider
{
    public string AnalyzerName => "standard";

    public bool IsAnalyzer(string analyzer)
    {
        return analyzer == AnalyzerName;
    }

    public AnalyzersDescriptor GetAnalyzerMapping(AnalyzersDescriptor aa, string analyzer)
    {
        return aa.Standard(AnalyzerName);
    }

}
