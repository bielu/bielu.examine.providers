namespace Bielu.Examine.Elasticsearch.Configuration;

public class AuthenticationDetails
{

    public string EndPoint { get; set; }
    public string AdminApiKey { get; set; }
    public string QueryApiKey { get; set; }
    public AuthenticationType AuthenticationType { get; set; }
}
