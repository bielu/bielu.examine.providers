using Bielu.Examine.Core.Models;
using Bielu.Examine.Core.Services;
using Bielu.Examine.Elasticsearch.Model;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Examine;
using Examine.Lucene.Search;
using Examine.Search;
using Lucene.Net.Analysis;
using Lucene.Net.Search;
using Microsoft.Extensions.Logging;
using Query = Elastic.Clients.Elasticsearch.QueryDsl.Query;

namespace Bielu.Examine.Elasticsearch.Providers;

public class ElasticsearchExamineSearcher(string name, string? indexAlias, ILoggerFactory loggerFactory, ISearchService elasticsearchService, IIndexStateService indexStateService) : BaseSearchProvider(name),IBieluExamineSearcher, IDisposable
{
    public string? IndexAlias => indexAlias;
    private readonly List<SortField> _sortFields = new List<SortField>();
    private string?[] _allFields;
    private IEnumerable<ExamineProperty>? _fieldsMapping;
    private ExamineIndexState IndexState => indexStateService.GetIndexState(name, elasticsearchService);

    private static readonly string[]? _emptyFields = Array.Empty<string>();
    public bool IndexExists => IndexState.Exist;


    public string[] AllFields
    {
        get
        {
            if (!IndexExists) return _emptyFields;

            IEnumerable<string> keys = AllProperties.Select(x => x.Key);

            _allFields = keys.ToArray();
            return _allFields;
        }
    }

    public IEnumerable<ExamineProperty> AllProperties
    {
        get
        {
            if (!IndexExists) return null;
            if (_fieldsMapping != null) return _fieldsMapping;

            _fieldsMapping = elasticsearchService.GetProperties(name);
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

    private BieluExamineSearchResults DoSearch(Query query, QueryOptions options,
        SortOptionsDescriptor<BieluExamineDocument>? optionsDescriptor = null)
    {
        SearchRequestDescriptor<BieluExamineDocument> searchDescriptor = new SearchRequestDescriptor<BieluExamineDocument>();
        searchDescriptor.Index(indexAlias)
            .Query(query);
        if (optionsDescriptor != null)
        {
            searchDescriptor = searchDescriptor.Sort(optionsDescriptor);
        }

        searchDescriptor = searchDescriptor.From(options.Skip).Size(options.Take);
        return elasticsearchService.Search(name,searchDescriptor);

    }


    private string[]? _parsedValues;
    private string[]? ParsedProperties
    {
        get
        {
            if (_parsedValues != null) return _parsedValues;
            _parsedValues = AllProperties?.Select(x => x.Key)?.ToArray() ?? _emptyFields;
            return _parsedValues;
        }
    }


    public override IQuery CreateQuery(string category = null,
        BooleanOperation defaultOperation = BooleanOperation.And) => elasticsearchService.CreateQuery(name, indexAlias, category, defaultOperation, null, null);

    public IQuery CreateQuery(string category, BooleanOperation defaultOperation, Analyzer? luceneAnalyzer, LuceneSearchOptions? searchOptions) => elasticsearchService.CreateQuery(name, indexAlias, category, defaultOperation, luceneAnalyzer, searchOptions);


#pragma warning disable CA1816
    public void Dispose() => loggerFactory.Dispose();
 #pragma warning restore CA1816
}
