using Bielu.Examine.Elasticsearch.Model;
using Elastic.Clients.Elasticsearch.Mapping;
using Examine;

namespace Bielu.Examine.Elasticsearch.Services;

public interface IPropertyMappingService
{
    Func<PropertiesDescriptor<BieluExamineDocument>, PropertiesDescriptor<BieluExamineDocument>>  GetElasticSearchMapping(ReadOnlyFieldDefinitionCollection properties, string analyzer);
}
