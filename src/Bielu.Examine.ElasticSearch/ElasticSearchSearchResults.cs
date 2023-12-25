using System.Collections;
using Examine;

namespace Bielu.Examine.ElasticSearch;

public class ElasticSearchSearchResults : ISearchResults
{
    public IEnumerator<ISearchResult> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public long TotalItemCount { get; }
}