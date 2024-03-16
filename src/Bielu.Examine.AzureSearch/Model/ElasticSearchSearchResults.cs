using Bielu.Examine.Core.Models;
using Examine;
using Examine.Lucene.Search;

namespace Bielu.Examine.Elasticsearch.Model;

public class AzureSearchSearchResults : BieluExamineSearchResults
{
    public AzureSearchSearchResults(IReadOnlyCollection<ISearchResult> results, long totalItemCount, double? maxscore, SearchAfterOptions afterOptions) : base(results, totalItemCount, maxscore, afterOptions)
    {
    }
    public static AzureSearchSearchResults Empty { get; } = new AzureSearchSearchResults(Array.Empty<ISearchResult>(), 0, float.NaN, default);
}
