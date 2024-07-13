using Azure.Search.Documents.Indexes.Models;
using Bielu.Examine.Core.Configuration;
using Bielu.Examine.Core.Extensions;
using Bielu.Examine.Elasticsearch.Model;
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
    protected virtual SearchFieldTemplate FromExamineType(FieldDefinition field, string analyzer)
    {
        var fieldType = field.Type.ToLowerInvariant();
        var fieldName = field.Name.FormatFieldName();
        if (fieldName.StartsWith('_'))
        {
            fieldName = $"s{fieldName}";
        }
        var azureSeachField = fieldType switch
        {
            var type when _dateFormats.Contains(type) => new SimpleField(fieldName, SearchFieldDataType.DateTimeOffset)
            {
                IsKey = false,
                IsFilterable = true,
                IsSortable = true,
            },
            "double" => new SimpleField(fieldName, SearchFieldDataType.Double)
            {
                IsKey = false,
                IsFilterable = true,
                IsSortable = true,
            },
            "float" => new SimpleField(fieldName, SearchFieldDataType.Double)
            {
                IsKey = false,
                IsFilterable = true,
                IsSortable = true,
            },
            "long" => new SimpleField(fieldName, SearchFieldDataType.Int64)
            {
                IsKey = false,
                IsFilterable = true,
                IsSortable = true,
            },
            var type when _integerFormats.Contains(type) => new SimpleField(fieldName, SearchFieldDataType.Int32)
            {
                IsKey = false,
                IsFilterable = true,
                IsSortable = true,
            },
            "raw" => new SearchableField(fieldName)
            {
                AnalyzerName = new LexicalAnalyzerName("keyword"),
                IsKey = false,
                IsFilterable = true,
                IsSortable = true
            },
            "keyword" => new SearchableField(fieldName)
            {
                AnalyzerName = new LexicalAnalyzerName("keyword"),
                IsKey = false,
                IsFilterable = true,
                IsSortable = true
            },
            _ => new SearchableField(fieldName)
            {
                AnalyzerName = new LexicalAnalyzerName("simple"),
                IsKey = false,
                IsFilterable = true,
                IsSortable = true
            }
        };

        return azureSeachField;
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
    public virtual IEnumerable<SearchFieldTemplate> GetAzureSearchMapping(ReadOnlyFieldDefinitionCollection properties, string analyzer)
    {
        var fields = new List<SearchFieldTemplate>();

        fields.Add(new SearchableField("Id")
        {
            AnalyzerName = new LexicalAnalyzerName("keyword"),
            IsKey = true,
            IsFilterable = true,
            IsSortable = true
        });
        fields.Add(new SearchableField(PrepareFieldName(ExamineFieldNames.ItemIdFieldName))
        {
            AnalyzerName = new LexicalAnalyzerName("keyword"),
            IsKey = false,
            IsFilterable = true,
            IsSortable = true
        });
        fields.Add(new SearchableField(PrepareFieldName(ExamineFieldNames.CategoryFieldName))
        {
            AnalyzerName = new LexicalAnalyzerName("keyword"),
            IsKey = false,
            IsFilterable = true,
            IsSortable = true
        });
        foreach (var mapping in configuration.FieldAnalyzerFieldMapping)
        {
            foreach (var propertyName in mapping.Value)
            {
                var name = PrepareFieldName(propertyName);
                var field = mapping.Key switch
                {
                    "keyword" => new SearchableField(name)
                    {
                        AnalyzerName = new LexicalAnalyzerName("keyword"),
                        IsKey = false,
                        IsFilterable = true,
                        IsSortable = true
                    },
                    "text" => new SearchableField(name)
                    {
                        AnalyzerName = new LexicalAnalyzerName("keyword"),
                        IsKey = false,
                        IsFilterable = true,
                        IsSortable = true
                    }, //todo: implement other types
                    _ => new SearchableField(name)
                    {
                        AnalyzerName = new LexicalAnalyzerName(mapping.Key),
                        IsKey = false,
                        IsFilterable = true,
                        IsSortable = true
                    }
                };
                fields.Add(field);
            }
        }
        foreach (FieldDefinition field in properties)
        {
            fields.Add(FromExamineType(field, analyzer));
        }

        return fields;
    }
    private static string PrepareFieldName(string fieldName)
    {
        if(fieldName.StartsWith('_'))
        {
            return $"s{fieldName.FormatFieldName()}";
        }
        return fieldName.FormatFieldName();
    }
}
