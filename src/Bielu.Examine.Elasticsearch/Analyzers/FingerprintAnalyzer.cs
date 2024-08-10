using Elastic.Clients.Elasticsearch.Analysis;

namespace Bielu.Examine.Elasticsearch.Services;

public class FingerprintAnalyzer : IAnalyzerProvider
{
    public string AnalyzerName => "fingerprint";

    public bool IsAnalyzer(string analyzer)
    {
        return analyzer == AnalyzerName;
    }

    public AnalyzersDescriptor GetAnalyzerMapping(AnalyzersDescriptor aa, string analyzer)
    {
        return aa.Fingerprint(AnalyzerName);
    }
}
