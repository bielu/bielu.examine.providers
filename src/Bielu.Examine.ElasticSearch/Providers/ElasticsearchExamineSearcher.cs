using Bielu.Examine.ElasticSearch.Configuration;
using Bielu.Examine.ElasticSearch.Extensions;
using Bielu.Examine.ElasticSearch.Helpers;
using Bielu.Examine.ElasticSearch.Model;
using Bielu.Examine.ElasticSearch.Queries;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Ingest;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport.Extensions;
using Examine;
using Examine.Lucene.Search;
using Examine.Search;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;
using Query = Elastic.Clients.Elasticsearch.QueryDsl.Query;

namespace Bielu.Examine.ElasticSearch.Providers;

public class ElasticsearchExamineSearcher : BaseSearchProvider, IDisposable
{
    private readonly ExamineElasticOptions _connectionConfiguration;
    public readonly Lazy<ElasticsearchClient> _client;
    internal readonly List<SortField> _sortFields = new List<SortField>();
    private string?[] _allFields;
    private Properties _fieldsMapping;
    private bool? _exists;
    private string _indexName;
    private string IndexName;

    public string indexAlias { get; set; }

    private string prefix
    {
        get
        {
            return _connectionConfiguration.IndexConfigurations.FirstOrDefault(x => x.Name == _indexName)?.Prefix ?? "";
        }
    }

    private static readonly string[] EmptyFields = new string[0];

    public ElasticsearchExamineSearcher(string name, string indexName, ILoggerFactory loggerFactory,
        ExamineElasticOptions connectionConfiguration) :
        base(name)
    {
        _indexName = name;
        _client = new Lazy<ElasticsearchClient>(() => CreateElasticSearchClient(indexName));
        indexAlias = prefix + Name;
        IndexName = indexName;
    }

    private ElasticsearchClient CreateElasticSearchClient(string indexName)
    {
        var serviceClient = new ElasticsearchClient();
        return serviceClient;
    }

    public bool IndexExists
    {
        get
        {
            _exists = _client.Value.IndexExists(indexAlias);
            return (bool)_exists;
        }
    }


    public string[] AllFields
    {
        get
        {
            if (!IndexExists) return EmptyFields;

            IEnumerable<PropertyName> keys = AllProperties.Select(x => x.Key);

            _allFields = keys.Select(x => x.Name).ToArray();
            return _allFields;
        }
    }

    public Properties AllProperties
    {
        get
        {
            if (!IndexExists) return null;
            if (_fieldsMapping != null) return _fieldsMapping;

            var indexesMappedToAlias = _client.Value.GetIndexesAssignedToAlias(indexAlias).ToList();
            GetMappingResponse response =
                _client.Value.Indices.GetMapping(new GetMappingRequest
                {
                });
            _fieldsMapping = response.GetMappingFor(indexesMappedToAlias[0]).Properties;
            return _fieldsMapping;
        }
    }
    public override ISearchResults Search(string searchText, QueryOptions options = null)
    {
        var query = new MultiMatchQuery
        {
            Query = searchText,
            Analyzer = "standard",
            Slop = 2,
            Type = TextQueryType.Phrase
        };

        return DoSearch(query, options);
    }

    private ElasticSearchSearchResults DoSearch(Query query, QueryOptions options,
        SortOptionsDescriptor<ElasticDocument>? optionsDescriptor = null)
    {
        SearchRequestDescriptor<ElasticDocument> searchDescriptor = new SearchRequestDescriptor<ElasticDocument>();
        searchDescriptor.Index(_indexName)
            .Query(query);
        if (optionsDescriptor != null)
        {
            searchDescriptor = searchDescriptor.Sort(optionsDescriptor);
        }

        searchDescriptor = searchDescriptor.From(options.Skip).Size(options.Take);

        var json = _client.Value.RequestResponseSerializer.SerializeToString(searchDescriptor);
        SearchResponse<ElasticDocument>
            searchResult = _client.Value.Search<ElasticDocument>(searchDescriptor.Explain());


        return ConvertToSearchResults(searchResult);
    }

    private ElasticSearchSearchResults ConvertToSearchResults(SearchResponse<ElasticDocument> searchResult)
    {
        //todo: figure out 
        var results = searchResult.Hits.Select(x =>
            new SearchResult(x.Id, (float)x.Score.Value, () => new Dictionary<string, List<string>>())).ToList();
        var totalItemCount = searchResult.Total;
        var maxscore = searchResult.MaxScore;
        var afterOptions = new SearchAfterOptions(Convert.ToInt32(searchResult.Hits.Last().Id),
            (float)searchResult.Hits.Last().Score.Value, null, 0);
        return new ElasticSearchSearchResults(results, totalItemCount, maxscore, afterOptions,
            searchResult.Aggregations);
    }

    public override IQuery CreateQuery(string category = null,
        BooleanOperation defaultOperation = BooleanOperation.And)
    {
        return new ElasticSearchQuery(new ElasticSearchQueryParser(LuceneVersion.LUCENE_CURRENT,_fieldsMapping.GetFields().ToArray(),new StandardAnalyzer(LuceneVersion.LUCENE_48)), category, new LuceneSearchOptions(), defaultOperation );
    }

    public void Dispose()
    {
    }
}