namespace Bielu.Examine.Elasticsearch.Configuration;

public class IndexConfiguration
{
    public string Name { get; set; }
    public string Analyzer { get; set; }
    public bool OverrideClientConfiguration { get; set; }
    public string EndPoint { get; set; }
    public string AdminApiKey { get; set; }
    public string QueryApiKey { get; set; }
    public string Prefix { get; set; }
}
