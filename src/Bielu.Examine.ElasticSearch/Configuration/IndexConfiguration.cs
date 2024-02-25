namespace Bielu.Examine.Elasticsearch.Configuration;

public class IndexConfiguration
{
    public string Name { get; set; }
    public string Analyzer { get; set; }
    public bool OverrideClientConfiguration { get; set; }
    public string ConnectionString { get; set; }
    public AuthenticationType AuthenticationType { get; set; }
    public AuthenticationDetails? AuthenticationDetails { get; set; }
    public string Prefix { get; set; }
}
