using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;

namespace Bielu.Examine.ElasticSearch.Extensions
{
    public static class ElasticFieldExtensions
    {
        public static IEnumerable<string> GetFields(this Properties fields)
        {
            return fields.GetFields();
        }
    }
}