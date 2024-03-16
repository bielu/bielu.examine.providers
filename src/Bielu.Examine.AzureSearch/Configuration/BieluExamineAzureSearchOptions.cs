using Bielu.Examine.Core.Constants;
using bielu.SchemaGenerator.Core.Attributes;
using Newtonsoft.Json;

namespace Bielu.Examine.Elasticsearch.Configuration;
[SchemaGeneration]
public class BieluExamineAzureSearchOptions
{
    public bool DevMode { get; set; }
    [SchemaPrefix]
    [JsonIgnore]
    public static string SectionName { get; set; } = $"{BieluExamineConstants.SectionPrefix}:AzureSearch";
    public List<IndexConfiguration?> IndexConfigurations { get; set; } = new List<IndexConfiguration?>();
    public IndexConfiguration DefaultIndexConfiguration { get; set; } = new IndexConfiguration()
    {
    };
}
