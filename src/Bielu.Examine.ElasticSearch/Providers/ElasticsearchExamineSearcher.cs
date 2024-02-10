using System.Globalization;
using Bielu.Examine.ElasticSearch.Queries;
using Bielu.Examine.Elasticsearch2.Extensions;
using Bielu.Examine.Elasticsearch2.Helpers;
using Bielu.Examine.Elasticsearch2.Configuration;
using Bielu.Examine.Elasticsearch2.Model;
using Bielu.Examine.Elasticsearch2.Services;
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
using Microsoft.Extensions.Options;
using Query = Elastic.Clients.Elasticsearch.QueryDsl.Query;

namespace Bielu.Examine.Elasticsearch2.Providers;

public class ElasticsearchExamineSearcher(string name, string? indexName, ILoggerFactory loggerFactory, IElasticSearchClientFactory clientFactory,
    IOptionsMonitor<BieluExamineElasticOptions> connectionConfiguration) : BaseSearchProvider(name), IDisposable
{
    public ElasticsearchClient Client
    {
        get
        {
            return clientFactory.GetOrCreateClient(indexName);
        }
    }
    private readonly List<SortField> _sortFields = new List<SortField>();
    private string?[] _allFields;
    private Properties _fieldsMapping;
    private bool? _exists;
    private string _indexName;

    public string? IndexAlias
    {
        get
        {
            return name;
        }
    }

    public string Prefix
    {
        get
        {
            return connectionConfiguration.CurrentValue.IndexConfigurations.FirstOrDefault(x => x.Name == _indexName)?.Prefix ?? "";
        }
    }

    private static readonly string[] _emptyFields = Array.Empty<string>();
    public bool IndexExists
    {
        get
        {
            _exists = Client.IndexExists(IndexAlias);
            return (bool)_exists;
        }
    }


    public string[] AllFields
    {
        get
        {
            if (!IndexExists) return _emptyFields;

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

            var indexesMappedToAlias = Client.GetIndexesAssignedToAlias(IndexAlias).ToList();
            GetMappingResponse response =
                Client.Indices.GetMapping(new GetMappingRequest
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

        var json = Client.RequestResponseSerializer.SerializeToString(searchDescriptor);
        SearchResponse<ElasticDocument>
            searchResult = Client.Search<ElasticDocument>(searchDescriptor.Explain());


        return ConvertToSearchResults(searchResult);
    }

    private static ElasticSearchSearchResults ConvertToSearchResults(SearchResponse<ElasticDocument> searchResult)
    {
        //todo: figure out
        var results = searchResult.Hits.Select(x =>
            new SearchResult(x.Id, (float)x.Score.Value, () => new Dictionary<string, List<string>>())).ToList();
        var totalItemCount = searchResult.Total;
        var maxscore = searchResult.MaxScore;
        var afterOptions = new SearchAfterOptions(Convert.ToInt32(searchResult.Hits.Last().Id,CultureInfo.InvariantCulture),
            (float)searchResult.Hits.Last().Score.Value, null, 0);
        return new ElasticSearchSearchResults(results, totalItemCount, maxscore, afterOptions,
            searchResult.Aggregations);
    }

    public override IQuery CreateQuery(string category = null,
        BooleanOperation defaultOperation = BooleanOperation.And)
    {
        return new ElasticSearchQuery(new ElasticSearchQueryParser(LuceneVersion.LUCENE_CURRENT,_fieldsMapping.GetFields().ToArray(),new StandardAnalyzer(LuceneVersion.LUCENE_48)), this,loggerFactory, category, new LuceneSearchOptions(), defaultOperation );
    }
 #pragma warning disable CA1816
    public void Dispose() => loggerFactory.Dispose();
 #pragma warning restore CA1816
}
