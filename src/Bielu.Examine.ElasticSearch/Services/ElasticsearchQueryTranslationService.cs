using Bielu.Examine.Core.Services;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Examine.Lucene.Indexing;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Documents;
using Microsoft.Extensions.Logging;

namespace Bielu.Examine.Elasticsearch2.Services;

public class ElasticsearchQueryTranslationService(ILoggerFactory loggerFactory) : IQueryTranslationService<PropertyName, IProperty>
{

    public IIndexFieldValueType FromSearchType(KeyValuePair<PropertyName, IProperty> propertyDescriptionPair)
    {
        switch (propertyDescriptionPair.Value.Type.ToLowerInvariant())
        {
            case "date":
                return new DateTimeType(propertyDescriptionPair.Key.Name, loggerFactory, DateResolution.MILLISECOND);
            case "double":
                return new DoubleType(propertyDescriptionPair.Key.Name, loggerFactory);

            case "float":
                return new SingleType(propertyDescriptionPair.Key.Name, loggerFactory);

            case "long":
                return new Int64Type(propertyDescriptionPair.Key.Name, loggerFactory);
            case "integer":
                return new Int32Type(propertyDescriptionPair.Key.Name, loggerFactory);
            default:
                return new FullTextType(propertyDescriptionPair.Key.Name, loggerFactory, PatternAnalyzer.DEFAULT_ANALYZER);
        }
    }
}
