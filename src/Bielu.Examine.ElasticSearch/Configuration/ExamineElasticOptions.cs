using Bielu.Examine.ElasticSearch.Constants;
using bielu.SchemaGenerator.Core.Attributes;
using Newtonsoft.Json;

namespace Bielu.Examine.ElasticSearch.Configuration;
[SchemaGeneration]
public class ExamineElasticOptions
{
    public bool DevMode { get; set; }
    [SchemaPrefix]
    [JsonIgnore]
    public static string SectionName { get; set; } = $"{BieluExamineConstants.SectionPrefix}";
    public List<IndexConfiguration> IndexConfigurations { get; set; } = new List<IndexConfiguration>();
}

public class IndexConfiguration
{
    public string Name { get; set; }
    public string Analyzer { get; set; }
    public string ConnectionString { get; set; }
    public AuthenticationType AuthenticationType { get; set; }
    public AuthenticationDetails AuthenticationDetails { get; set; }
    public string Prefix { get; set; }
}

public class AuthenticationDetails
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string ApiKey { get; set; }
    public string Id { get; set; }
}

public enum AuthenticationType
{
    None,
    Cloud,
    CloudApi
}