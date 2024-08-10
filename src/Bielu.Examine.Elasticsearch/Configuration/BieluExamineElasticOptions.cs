using Bielu.Examine.Core.Constants;
using Bielu.Examine.Elasticsearch.Constants;
using bielu.SchemaGenerator.Core.Attributes;
using Newtonsoft.Json;

namespace Bielu.Examine.Elasticsearch.Configuration;
[SchemaGeneration]
public class BieluExamineElasticOptions
{
    public bool DevMode { get; set; }
    [SchemaPrefix]
    [JsonIgnore]
    public static string SectionName { get; set; } = $"{BieluExamineElasticConstants.SectionPrefix}";
    public List<IndexConfiguration?> IndexConfigurations { get; set; } = new List<IndexConfiguration?>();
    public IndexConfiguration DefaultIndexConfiguration { get; set; } = new IndexConfiguration()
    {
        AuthenticationType = AuthenticationType.None,
        ConnectionString = "http://localhost:9200",
    };
}
