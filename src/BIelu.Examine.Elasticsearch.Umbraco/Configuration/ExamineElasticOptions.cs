using Bielu.Examine.Core.Constants;
using bielu.SchemaGenerator.Core.Attributes;
using Newtonsoft.Json;

namespace BIelu.Examine.Umbraco.Configuration;
[SchemaGeneration]
public class UmbracoExamineElasticOptions
{
    public bool DevMode { get; set; }
    [SchemaPrefix]
    [JsonIgnore]
    public static string SectionName { get; set; } = $"{BieluExamineConstants.SectionPrefix}:Umbraco";
}