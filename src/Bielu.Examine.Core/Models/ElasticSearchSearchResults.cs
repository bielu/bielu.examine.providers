using Examine;
using Examine.Lucene.Search;

namespace Bielu.Examine.Core.Models;

public class BieluExamineSearchResults : LuceneSearchResults
{
    public BieluExamineSearchResults(IReadOnlyCollection<ISearchResult> results, long totalItemCount, double? maxscore, SearchAfterOptions afterOptions) : base(results, (int)totalItemCount, (float)maxscore.Value, afterOptions)
    {
    }
    public static BieluExamineSearchResults Empty { get; } = new BieluExamineSearchResults(Array.Empty<ISearchResult>(), 0, float.NaN, default);

}
