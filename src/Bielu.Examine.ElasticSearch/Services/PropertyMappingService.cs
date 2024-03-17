using Bielu.Examine.Core.Configuration;
using Bielu.Examine.Core.Extensions;
using Bielu.Examine.Elasticsearch.Model;
using Elastic.Clients.Elasticsearch.Mapping;
using Examine;
using Lucene.Net.Analysis;

namespace Bielu.Examine.Elasticsearch.Services;

public class PropertyMappingService(BieluExamineConfiguration configuration) : IPropertyMappingService
{
    private static readonly string[] _dateFormats = new[]
    {
        "date", "datetimeoffset", "datetime"
    };
    private static readonly string[] _integerFormats = new[]
    {
        "int", "number"
    };
  protected virtual void FromExamineType(ref PropertiesDescriptor<BieluExamineDocument> descriptor, FieldDefinition field, string analyzer)
    {
        var fieldType = field.Type.ToLowerInvariant();
        var fieldName = field.Name.FormatFieldName();
        descriptor = fieldType switch
        {
            var type when _dateFormats.Contains(type) => descriptor.Date(fieldName),
            "double" => descriptor.DoubleNumber(fieldName),
            "float" => descriptor.FloatNumber(fieldName),
            "long" => descriptor.LongNumber(fieldName),
            var type when _integerFormats.Contains(type) => descriptor.IntegerNumber(fieldName),
            "raw" => descriptor.Keyword(fieldName),
            "keyword" => descriptor.Keyword(fieldName),
            _ => descriptor.Text(fieldName, configure => configure.Analyzer(FromLuceneAnalyzer(analyzer)))
        };
    }
    private static string FromLuceneAnalyzer(string? analyzer)
    {
        return analyzer switch
        {
            null or "" => "simple",
            _ when !analyzer.Contains(',') => "simple",
            _ when analyzer.Contains("StandardAnalyzer") => "standard",
            _ when analyzer.Contains("WhitespaceAnalyzer") => "whitespace",
            _ when analyzer.Contains("SimpleAnalyzer") => "simple",
            _ when analyzer.Contains("KeywordAnalyzer") => "keyword",
            _ when analyzer.Contains("StopAnalyzer") => "stop",
            _ when analyzer.Contains("ArabicAnalyzer") => "arabic",
            _ when analyzer.Contains("BrazilianAnalyzer") => "brazilian",
            _ when analyzer.Contains("ChineseAnalyzer") => "chinese",
            _ when analyzer.Contains("CJKAnalyzer") => "cjk",
            _ when analyzer.Contains("CzechAnalyzer") => "czech",
            _ when analyzer.Contains("DutchAnalyzer") => "dutch",
            _ when analyzer.Contains("FrenchAnalyzer") => "french",
            _ when analyzer.Contains("GermanAnalyzer") => "german",
            _ when analyzer.Contains("RussianAnalyzer") => "russian",
            _ => "simple"
        };
    }
    public virtual PropertiesDescriptor<BieluExamineDocument> CreateFieldsMapping(PropertiesDescriptor<BieluExamineDocument> descriptor,
        ReadOnlyFieldDefinitionCollection fieldDefinitionCollection, string analyzer)
    {

        descriptor.Keyword(s => "Id");
        descriptor.Keyword(s => ExamineFieldNames.ItemIdFieldName.FormatFieldName());
        descriptor.Keyword(s => ExamineFieldNames.ItemTypeFieldName.FormatFieldName());
        descriptor.Keyword(s => ExamineFieldNames.CategoryFieldName.FormatFieldName());
        foreach (var mapping in configuration.FieldAnalyzerFieldMapping)
        {
            foreach (var propertyName in mapping.Value)
            {
                descriptor = mapping.Key switch
                {
                    "keyword" => descriptor.Keyword(s => propertyName),
                    "text" => descriptor.Text(s => propertyName, configure => configure.Analyzer(FromLuceneAnalyzer(analyzer))), //todo: implement other types
                    _ => descriptor.Text(s => propertyName, configure => configure.Analyzer(FromLuceneAnalyzer(analyzer)))
                };
            }
        }
        foreach (FieldDefinition field in fieldDefinitionCollection)
        {
            FromExamineType(ref descriptor, field,analyzer);
        }

        return descriptor;
    }
    public Func<PropertiesDescriptor<BieluExamineDocument>, PropertiesDescriptor<BieluExamineDocument>> GetElasticSearchMapping(ReadOnlyFieldDefinitionCollection properties, string analyzer) => (descriptor) => CreateFieldsMapping(descriptor, properties, analyzer);
}
