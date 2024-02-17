using System.Text.RegularExpressions;
using Bielu.Examine.Core.Queries;
using Bielu.Examine.Core.Regex;
using Bielu.Examine.Elasticsearch.Extensions;
using Bielu.Examine.Elasticsearch.Model;
using Bielu.Examine.Elasticsearch.Providers;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport.Extensions;
using Examine;
using Examine.Lucene.Indexing;
using Examine.Lucene.Search;
using Examine.Search;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;
using FuzzyQuery = Lucene.Net.Search.FuzzyQuery;
using KeywordAnalyzer = Lucene.Net.Analysis.Core.KeywordAnalyzer;
using PatternAnalyzer = Lucene.Net.Analysis.Miscellaneous.PatternAnalyzer;
using Query = Lucene.Net.Search.Query;
using WildcardQuery = Lucene.Net.Search.WildcardQuery;

namespace Bielu.Examine.Elasticsearch.Queries;

public partial class ElasticSearchQuery(
    CustomMultiFieldQueryParser queryParser,
    ElasticsearchExamineSearcher searcher,
    ILoggerFactory loggerFactory,
    ILogger<ElasticSearchQuery> logger,
    string category,
    LuceneSearchOptions searchOptions,
    BooleanOperation occurance) : BieluExamineBaseQuery(queryParser, logger, category, searchOptions, occurance)
{


    internal Stack<BooleanQuery> ____RULE_VIOLATION____Queries____RULE_VIOLATION____ = new Stack<BooleanQuery>();
    private static readonly LuceneSearchOptions _emptyOptions = new LuceneSearchOptions();

    private QueryStringQuery? _queryContainer;
    private SearchRequest<Document>? _searchRequest;
    private Func<SearchRequestDescriptor<ElasticDocument>, SearchRequest<Document>>? _searchSelector;
    private Action<SortOptionsDescriptor<ElasticDocument>> _sortDescriptor;
    public override ISearchResults Execute(QueryOptions? options)
    {
        var searchResult = DoSearch(options);
        if (!searchResult.IsSuccess())
        {
 #pragma warning disable CA1848
            logger.LogError("Failed to execute search query: {DebugInfo}", searchResult.DebugInformation);
 #pragma warning restore CA1848
        }
        return searchResult.ConvertToSearchResults();
    }
    private SearchResponse<ElasticDocument> DoSearch(QueryOptions? options)
    {
        SearchResponse<ElasticDocument> searchResult;
        var query = ExtractQuery();
        if (query != null)
        {
            _queryContainer =
                new QueryStringQuery()
                {
                    Query = QueryRegex.PathRegex().Replace(Query.ToString(), "$1\\-"), AnalyzeWildcard = true

                };

        }

        if (_queryContainer != null)
        {
            SearchRequestDescriptor<ElasticDocument> searchDescriptor = new SearchRequestDescriptor<ElasticDocument>();
            searchDescriptor.Index(searcher.IndexAlias)
                .Size(options.Take)
                .From(options.Skip)
                .Query(_queryContainer)
                .Sort(_sortDescriptor);

            var json = searcher.Client.RequestResponseSerializer.SerializeToString(searchDescriptor);
            searchResult = searcher.Client.Search<ElasticDocument>(searchDescriptor);
        }
        else if (_searchRequest != null)
        {
            searchResult = searcher.Client.Search<ElasticDocument>(_searchRequest);
        }
        else
        {
            searchResult = searcher.Client.Search<ElasticDocument>(_searchSelector.Invoke(new SearchRequestDescriptor<ElasticDocument>()));
        }


        return searchResult;
    }

    protected override LuceneBooleanOperationBase CreateOp()
    {
        return new ElasticSearchBooleanOperation(this);
    }
    internal override BieluExamineBooleanOperation RangeQueryInternal<T>(string[] fields, T? min, T? max,
        bool minInclusive = true, bool maxInclusive = true)
        where T : struct
    {
        Query.Add(new LateBoundQuery(() =>
        {
            //Strangely we need an inner and outer query. If we don't do this then the lucene syntax returned is incorrect
            //since it doesn't wrap in parenthesis properly. I'm unsure if this is a lucene issue (assume so) since that is what
            //is producing the resulting lucene string syntax. It might not be needed internally within Lucene since it's an object
            //so it might be the ToString() that is the issue.
            var outer = new BooleanQuery();
            var inner = new BooleanQuery();

            var fieldsMapping = searcher.AllProperties;

            foreach (var valueType in fieldsMapping.Where(e => fields.Contains(e.Key.Name)))
            {

                if (FromEngineType(valueType) is IIndexRangeValueType type)
                {
                    var q = ((IIndexRangeValueType<T>)type).GetQuery(min, max, minInclusive, maxInclusive);
                    if (q != null)
                    {
                        //CriteriaContext.FieldQueries.Add(new KeyValuePair<IIndexFieldValueType, Query>(type, q));
                        inner.Add(q, Occur.SHOULD);
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Could not perform a range query on the field {valueType.Key.Name}, it's value type is {valueType.Value.Type}");
                }
            }

            outer.Add(inner, Occur.SHOULD);

            return outer;
        }), Occurrence);


        return new ElasticSearchBooleanOperation(this);
    }

    public override IEnumerable<string> GetAllProperties()
    {
        return searcher.AllProperties.Where(x => x.Value.Type == "text").Select(x => x.Key.Name);
    }
    public override IIndexFieldValueType FromEngineType<TPropertyName, TProperty>(KeyValuePair<TPropertyName, TProperty> propetyField)
    {
        if (propetyField is not KeyValuePair<PropertyName, IProperty> elasticProperty)
        {
            throw new ArgumentException("The property must be a KeyValuePair<PropertyName, IProperty>", nameof(propetyField));
        }
        switch (elasticProperty.Value.Type.ToLowerInvariant())
        {
            case "date":
                return new DateTimeType(elasticProperty.Key.Name, loggerFactory, DateResolution.MILLISECOND);
            case "double":
                return new DoubleType(elasticProperty.Key.Name, loggerFactory);

            case "float":
                return new SingleType(elasticProperty.Key.Name, loggerFactory);

            case "long":
                return new Int64Type(elasticProperty.Key.Name, loggerFactory);
            case "integer":
                return new Int32Type(elasticProperty.Key.Name, loggerFactory);
            default:
                return new FullTextType(elasticProperty.Key.Name, loggerFactory, PatternAnalyzer.DEFAULT_ANALYZER);
        }
    }
}
