using Bielu.Examine.Core.Constants;
using bielu.SchemaGenerator.Core.Attributes;
using Newtonsoft.Json;

namespace Bielu.Examine.Core.Configuration;
[SchemaGeneration]
public class BieluExamineOptions
{
    [SchemaPrefix]
    [JsonIgnore]
    public static string SectionName { get; set; } = $"{BieluExamineConstants.SectionPrefix}";
    public bool Enabled { get; set; }
}
