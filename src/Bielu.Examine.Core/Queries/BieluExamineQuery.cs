using Bielu.Examine.Core.Models;
using Bielu.Examine.Core.Regex;
using Bielu.Examine.Core.Services;
using Bielu.Examine.Elasticsearch;
using Examine;
using Examine.Lucene.Indexing;
using Examine.Lucene.Search;
using Examine.Search;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Microsoft.Extensions.Logging;
using PatternAnalyzer = Lucene.Net.Analysis.Miscellaneous.PatternAnalyzer;

namespace Bielu.Examine.Core.Queries;

public partial class BieluExamineQuery(
    string indexName,
    string indexAliast,
    CustomMultiFieldQueryParser queryParser,
    ISearchService elasticsearchService,
    ILoggerFactory loggerFactory,
    ILogger<BieluExamineQuery> logger,
    string category,
    LuceneSearchOptions searchOptions,
    BooleanOperation occurance) : BieluExamineBaseQuery(queryParser, logger, category, searchOptions, occurance)
{


    internal Stack<BooleanQuery> Queries { get; } = new Stack<BooleanQuery>();
    private static readonly LuceneSearchOptions _emptyOptions = new LuceneSearchOptions();
    public override ISearchResults Execute(QueryOptions? options) => DoSearch(options);
    private BieluExamineSearchResults DoSearch(QueryOptions? options)
    {
        var query = ExtractQuery();
        BieluExamineSearchResults searchResult;
        if (query != null)
        {
       return  elasticsearchService.Search(indexName, options, query);
        }
        return BieluExamineSearchResults.Empty;
    }

    protected override LuceneBooleanOperationBase CreateOp()
    {
        return new BieluExamineBooleanOperation(this);
    }
    internal override BieluExamineBooleanOperationBase RangeQueryInternal<T>(string[] fields, T? min, T? max,
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
                        $"Could not perform a range query on the field {valueType.Key}, it's value type is {valueType.Type}");
                }
            }

            outer.Add(inner, Occur.SHOULD);

            return outer;
        }), Occurrence);


        return new BieluExamineBooleanOperation(this);
    }

    public override IEnumerable<string> GetAllProperties()
    {
        return elasticsearchService.GetProperties(indexName).Where(x => x.Type == "text").Select(x => x.Key);
    }
    public override IIndexFieldValueType FromEngineType(ExamineProperty propetyField)
    {
        switch (propetyField.Type)
        {
            case "date":
                return new DateTimeType(propetyField.Key, loggerFactory, DateResolution.MILLISECOND);
            case "double":
                return new DoubleType(propetyField.Key, loggerFactory);

            case "float":
                return new SingleType(propetyField.Key, loggerFactory);

            case "long":
                return new Int64Type(propetyField.Key, loggerFactory);
            case "integer":
                return new Int32Type(propetyField.Key, loggerFactory);
            default:
                return new FullTextType(propetyField.Key, loggerFactory, PatternAnalyzer.DEFAULT_ANALYZER);
        }
    }
}
