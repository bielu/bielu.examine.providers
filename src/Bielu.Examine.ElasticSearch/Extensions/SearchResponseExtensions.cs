using System.Globalization;
using Bielu.Examine.Elasticsearch.Model;
using Elastic.Clients.Elasticsearch;
using Examine;
using Examine.Lucene.Search;

namespace Bielu.Examine.Elasticsearch.Extensions;

public static class SearchResponseExtensions
{
    public static ElasticSearchSearchResults ConvertToSearchResults(this SearchResponse<ElasticDocument> searchResult)
    {
        //todo: figure out
        var results = searchResult.Hits.Select(x =>
            new SearchResult(x.Id, (float)x.Score.Value, () => new Dictionary<string, List<string>>())).ToList();
        var totalItemCount = searchResult.Total;
        var maxscore = searchResult.MaxScore;
        var afterOptions = new SearchAfterOptions(Convert.ToInt32(searchResult.Hits.Last().Fields["Id"],CultureInfo.InvariantCulture),
            (float)searchResult.Hits.Last().Score.Value, null, 0);
        return new ElasticSearchSearchResults(results, totalItemCount, maxscore, afterOptions,
            searchResult.Aggregations);
    }
}
