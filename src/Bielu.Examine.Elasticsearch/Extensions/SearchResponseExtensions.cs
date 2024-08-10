using System.Globalization;
using Bielu.Examine.Core.Models;
using Bielu.Examine.Elasticsearch.Model;
using Elastic.Clients.Elasticsearch;
using Examine;
using Examine.Lucene.Search;

namespace Bielu.Examine.Elasticsearch.Extensions;

public static class SearchResponseExtensions
{
    public static ElasticSearchSearchResults ConvertToSearchResults(this SearchResponse<BieluExamineDocument> searchResult)
    {
        if (!searchResult.IsSuccess())
        {
            return ElasticSearchSearchResults.Empty;
        }
        var results = searchResult.Hits.Select(x =>
            new SearchResult(x.Id, (float)x.Score.Value, () => x.Source?.ToDictionary(field => field.Key, field => field.Value is IEnumerable<object> list ? list.Select(item => item.ToString()).ToList() : new List<string?>()
            {
                field.Value.ToString()
            }))).ToList();
        var totalItemCount = searchResult.Total;
        var maxscore = searchResult.MaxScore ?? 0;
        var afterOptions = searchResult.Hits.Count != 0 ? new SearchAfterOptions(Convert.ToInt32(searchResult.Hits.Last().Id, CultureInfo.InvariantCulture),
            (float)searchResult.Hits.Last().Score!.Value , null, 0) : new SearchAfterOptions(0, 0, null, 0);
        return new ElasticSearchSearchResults(results, totalItemCount, maxscore, afterOptions,
            searchResult.Aggregations);
    }
}
