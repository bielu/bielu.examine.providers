using Bielu.Examine.Core.Constants;
using Bielu.Examine.ElasticSearch.Constants;
using bielu.SchemaGenerator.Core.Attributes;
using Newtonsoft.Json;

namespace Bielu.Examine.ElasticSearch.Configuration;
[SchemaGeneration]
public class BieluExamineElasticOptions
{
    public bool DevMode { get; set; }
    [SchemaPrefix]
    [JsonIgnore]
    public static string SectionName { get; set; } = $"{BieluExamineConstants.SectionPrefix}";
    public List<IndexConfiguration?> IndexConfigurations { get; set; } = new List<IndexConfiguration?>();
}
