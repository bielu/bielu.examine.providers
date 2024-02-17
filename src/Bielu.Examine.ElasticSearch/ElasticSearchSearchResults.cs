using Elastic.Clients.Elasticsearch.Aggregations;
using Examine;
using Examine.Lucene.Search;

namespace Bielu.Examine.Elasticsearch;

public class ElasticSearchSearchResults : LuceneSearchResults
{
    public ElasticSearchSearchResults(IReadOnlyCollection<ISearchResult> results, long totalItemCount, double? maxscore, SearchAfterOptions afterOptions, AggregateDictionary aggregateDictionary) : base(results, (int)totalItemCount, (float)maxscore.Value, afterOptions)
    {
        Aggregation = aggregateDictionary;
    }
    public static ElasticSearchSearchResults Empty { get; } = new ElasticSearchSearchResults(Array.Empty<ISearchResult>(), 0, float.NaN, default, null);
    public AggregateDictionary Aggregation { get; }

}
