using Elastic.Clients.Elasticsearch;

namespace Bielu.Examine.ElasticSearch.Helpers;

public static class ElasticSearchHelper
{
    public static bool IndexExists(this ElasticsearchClient client,string indexName)
    {
        var aliasExists = client.Indices.Exists(indexName).Exists;
        if (aliasExists)
        {
            var indexesMappedToAlias = client.Indices.Get(indexName).Indices;
            if (indexesMappedToAlias.Count > 0)
            {
                return true;
            }
        }

        return false;
    }
    public static IList<string> GetIndexesAssignedToAlias(this ElasticsearchClient client, string aliasName)
    {
        var aliasExists = client.Indices.Exists(aliasName).Exists;
        if (aliasExists)
        {
            var indexesMappedToAlias = client.Indices.Get(aliasName).Indices;
            if (indexesMappedToAlias.Count > 0)
            {
                return indexesMappedToAlias.Keys.Select(x=>x.ToString()).ToList();
            }
        }

        return new List<string>();
    }
}