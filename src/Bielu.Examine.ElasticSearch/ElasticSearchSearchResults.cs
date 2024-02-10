using System.Collections;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Examine;
using Examine.Lucene.Search;
using Examine.Search;
using Lucene.Net.Search;

namespace Bielu.Examine.Elasticsearch2;

public class ElasticSearchSearchResults : LuceneSearchResults
{
    public AggregateDictionary Aggregation { get; }
    public ElasticSearchSearchResults(IReadOnlyCollection<ISearchResult> results, long totalItemCount, double? maxscore, SearchAfterOptions afterOptions, AggregateDictionary aggregateDictionary) : base(results, (int)totalItemCount, (float)maxscore.Value, afterOptions)
    {
        Aggregation = aggregateDictionary;
    }
}