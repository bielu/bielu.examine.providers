using Azure.Search.Documents.Indexes.Models;
using Bielu.Examine.Elasticsearch.Model;
using Examine;

namespace Bielu.Examine.Elasticsearch.Services;

public interface IPropertyMappingService
{
    IEnumerable<SearchFieldTemplate>  GetAzureSearchMapping(ReadOnlyFieldDefinitionCollection properties, string analyzer);
}
