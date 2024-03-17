using System.Globalization;
using Azure.Search.Documents.Models;
using Bielu.Examine.Core.Models;
using Bielu.Examine.Elasticsearch.Model;
using Examine;
using Examine.Lucene.Search;

namespace Bielu.Examine.Elasticsearch.Extensions;

public static class SearchResponseExtensions
{
    public static AzureSearchSearchResults ConvertToSearchResults(this SearchResults<ElasticDocument> searchResult)
    {
        var results = searchResult.GetResults();
        if (!results.Any())
        {
            return AzureSearchSearchResults.Empty;
        }
        var umbracoResults = results.Select(x =>
            new SearchResult(x.Document["Id"].ToString(), (float)x.Score.Value, () => x.Document?.ToDictionary(field => field.Key, field => field.Value is IEnumerable<object> list ? list.Select(item => item.ToString()).ToList() : new List<string?>()
            {
                field.Value.ToString()
            }))).ToList();
        var totalItemCount = searchResult.TotalCount ?? 0;
        var maxscore = results.Max(x=>x.Score) ?? 0;
        var lastDocument = results.Last();
        var afterOptions = results.Any() ? new SearchAfterOptions(Convert.ToInt32(lastDocument.Document["Id"], CultureInfo.InvariantCulture),
            (float)lastDocument.Score!.Value , null, 0) : new SearchAfterOptions(0, 0, null, 0);
        return new AzureSearchSearchResults(umbracoResults, totalItemCount, maxscore, afterOptions);
    }
}
