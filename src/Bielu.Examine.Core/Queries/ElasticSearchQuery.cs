using Bielu.Examine.Core.Models;
using Bielu.Examine.Core.Regex;
using Bielu.Examine.Core.Services;
using Examine;
using Examine.Lucene.Indexing;
using Examine.Lucene.Search;
using Examine.Search;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Microsoft.Extensions.Logging;
using PatternAnalyzer = Lucene.Net.Analysis.Miscellaneous.PatternAnalyzer;

namespace Bielu.Examine.Core.Queries;

public partial class ElasticSearchQuery(
    string indexName,
    string indexAliast,
    CustomMultiFieldQueryParser queryParser,
    ISearchService elasticsearchService,
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
    public override ISearchResults Execute(QueryOptions? options) => DoSearch(options);
    private BieluExamineSearchResults DoSearch(QueryOptions? options)
    {
        BieluExamineSearchResults searchResult;
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
            searchDescriptor.Index(indexAliast)
                .Size(options?.Take ?? 1000)
                .From(options?.Skip ?? 0)
                .Query(_queryContainer)
                .Sort(_sortDescriptor);
            searchResult = elasticsearchService.Search(indexName, searchDescriptor);
        }
        else if (_searchRequest != null)
        {
            searchResult = elasticsearchService.Search(indexName, _searchRequest);
        }
        else
        {
            searchResult = elasticsearchService.Search(indexName, (_searchSelector.Invoke(new SearchRequestDescriptor<ElasticDocument>())));
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

            var fieldsMapping = elasticsearchService.GetProperties(indexName);

            foreach (var valueType in fieldsMapping.Where(e => fields.Contains(e.Key)))
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
                        $"Could not perform a range query on the field {valueType.Key}, it's value type is {valueType.Value}");
                }
            }

            outer.Add(inner, Occur.SHOULD);

            return outer;
        }), Occurrence);


        return new ElasticSearchBooleanOperation(this);
    }

    public override IEnumerable<string> GetAllProperties()
    {
        return elasticsearchService.GetProperties(indexName).Where(x => x.Value == "text").Select(x => x.Key);
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
