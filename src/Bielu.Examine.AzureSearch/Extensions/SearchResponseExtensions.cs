using System.Globalization;
using Azure.Search.Documents.Models;
using Bielu.Examine.Core.Models;
using Bielu.Examine.Elasticsearch.Model;
using Examine;
using Examine.Lucene.Search;

namespace Bielu.Examine.Elasticsearch.Extensions;

public static class SearchResponseExtensions
{
    public static AzureSearchSearchResults ConvertToSearchResults(this SearchResultsPage<ElasticDocument> searchResult)
    {
        if (!searchResult.Values.Any())
        {
            return AzureSearchSearchResults.Empty;
        }
        var results = searchResult.Values.Select(x =>
            new SearchResult(x.Document["Id"].ToString(), (float)x.Score.Value, () => x.Document?.ToDictionary(field => field.Key, field => field.Value is IEnumerable<object> list ? list.Select(item => item.ToString()).ToList() : new List<string?>()
            {
                field.Value.ToString()
            }))).ToList();
        var totalItemCount = searchResult.TotalCount ?? 0;
        var maxscore = searchResult.Values.Max(x=>x.Score) ?? 0;
        var lastDocument = searchResult.Values[^1];
        var afterOptions = searchResult.Values.Count != 0 ? new SearchAfterOptions(Convert.ToInt32(lastDocument.Document["Id"], CultureInfo.InvariantCulture),
            (float)lastDocument.Score!.Value , null, 0) : new SearchAfterOptions(0, 0, null, 0);
        return new AzureSearchSearchResults(results, totalItemCount, maxscore, afterOptions);
    }
}
