using Elastic.Clients.Elasticsearch.Mapping;

namespace Bielu.Examine.Elasticsearch.Extensions
{
    public static class ElasticFieldExtensions
    {
        public static IEnumerable<string> GetFields(this Properties fields)
        {
            return fields.GetFields();
        }
    }
}