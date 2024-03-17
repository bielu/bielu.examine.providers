using Microsoft.Extensions.DependencyInjection;

namespace Bielu.Examine.Core.Configuration;

public class BieluExamineConfigurator(BieluExamineConfiguration bieluExamineConfiguration, IServiceCollection collection)
{
    public IServiceCollection ServiceCollection { get; } = collection;
    public BieluExamineConfigurator AddFieldAnalyzerFieldMapping(string field, string analyzer)
    {
        if (bieluExamineConfiguration.FieldAnalyzerFieldMapping.TryGetValue(field, out var value))
        {
            value.Add(analyzer);
        }
        else
        {
            bieluExamineConfiguration.FieldAnalyzerFieldMapping.Add(field, new List<string> {analyzer});
        }
        return this;
    }
    public Type OptionsType { get; set; }

}
