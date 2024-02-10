using System.Collections;
using Bielu.Examine.Elasticsearch2;
using Bielu.Examine.Elasticsearch2.Providers;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Analysis;
using Elastic.Clients.Elasticsearch.Mapping;
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
using KeywordAnalyzer = Lucene.Net.Analysis.Core.KeywordAnalyzer;
using PatternAnalyzer = Lucene.Net.Analysis.Miscellaneous.PatternAnalyzer;

namespace Bielu.Examine.ElasticSearch.Queries;

public class ElasticSearchQuery(CustomMultiFieldQueryParser queryParser, ElasticsearchExamineSearcher searcher, ILoggerFactory loggerFactory,
    string category, LuceneSearchOptions searchOptions, BooleanOperation occurance) : LuceneSearchQueryBase(queryParser, category, searchOptions, occurance), IQueryExecutor, IQuery
{


    internal Stack<BooleanQuery> ____RULE_VIOLATION____Queries____RULE_VIOLATION____ = new Stack<BooleanQuery>();
    private readonly List<SortField> _sortFields = new List<SortField>();
    private static readonly LuceneSearchOptions _emptyOptions = new LuceneSearchOptions();
    private readonly LuceneVersion _luceneVersion = LuceneVersion.LUCENE_48;


    public ISearchResults Execute(QueryOptions? options)
    {
      //  searcher.Client.Search();
      //  return new ElasticSearchSearchResults();
      return null;
    }

    protected override LuceneBooleanOperationBase CreateOp()
    {
        return new ElasticSearchBooleanOperation(this);
    }

    public override IBooleanOperation Field<T>(string fieldName, T fieldValue)
        => RangeQueryInternal<T>(new[]
        {
            fieldName
        }, fieldValue, fieldValue);
    public IBooleanOperation Field(string fieldName, IExamineValue fieldValue)
        => FieldInternal(fieldName, fieldValue, Occurrence);
    public override IBooleanOperation ManagedQuery(string query, string[]? fields = null)
    {
        //TODO: Instead of AllFields here we should have a reference to the FieldDefinitionCollection
        var fielddefintion =
            searcher.AllProperties.Where(x => x.Value.Type == "text").Select(x => x.Key.Name);
        foreach (var field in fields ?? fielddefintion)
        {
            var fullTextQuery = FullTextType.GenerateQuery(field, query, PatternAnalyzer.DEFAULT_ANALYZER);
            Query.Add(fullTextQuery, Occur.SHOULD);
        }

        return new ElasticSearchBooleanOperation(this);
    }


    public override IBooleanOperation RangeQuery<T>(string[] fields, T? min, T? max, bool minInclusive = true,
        bool maxInclusive = true) => RangeQueryInternal(fields, min, max, minInclusive, maxInclusive);

    protected override INestedBooleanOperation FieldNested<T>(string fieldName, T fieldValue)
        => RangeQueryInternal<T>(new[]
        {
            fieldName
        }, fieldValue, fieldValue);

    protected override INestedBooleanOperation ManagedQueryNested(string query, string[]? fields = null)
    {

        //TODO: Instead of AllFields here we should have a reference to the FieldDefinitionCollection
        var fielddefintion =
            searcher.AllProperties.Where(x => x.Value.Type == "text").Select(x => x.Key.Name);
        foreach (var field in fields ?? fielddefintion)
        {
            var fullTextQuery = FullTextType.GenerateQuery(field, query, PatternAnalyzer.DEFAULT_ANALYZER);
            Query.Add(fullTextQuery, Occurrence);
        }

        return new ElasticSearchBooleanOperation(this);
    }

    protected override INestedBooleanOperation RangeQueryNested<T>(string[] fields, T? min, T? max,
        bool minInclusive = true,
        bool maxInclusive = true)
        => RangeQueryInternal(fields, min, max, minInclusive, maxInclusive);

    private IIndexFieldValueType FromElasticType(KeyValuePair<PropertyName, IProperty> property)
    {
        switch (property.Value.Type.ToLowerInvariant())
        {
            case "date":
                return new DateTimeType(property.Key.Name, loggerFactory, DateResolution.MILLISECOND);
            case "double":
                return new DoubleType(property.Key.Name, loggerFactory);

            case "float":
                return new SingleType(property.Key.Name, loggerFactory);

            case "long":
                return new Int64Type(property.Key.Name, loggerFactory);
            case "integer":
                return new Int32Type(property.Key.Name, loggerFactory);
            default:
                return new FullTextType(property.Key.Name, loggerFactory, PatternAnalyzer.DEFAULT_ANALYZER);
        }
    }

    internal ElasticSearchBooleanOperation RangeQueryInternal<T>(string[] fields, T? min, T? max,
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

                if (FromElasticType(valueType) is IIndexRangeValueType type)
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
    public new IBooleanOperation GroupedAnd(IEnumerable<string> fields, params string[] query)
        => this.GroupedAndInternal(fields.ToArray(), query.Select(f => new ExamineValue(Examineness.Explicit, f)).Cast<IExamineValue>().ToArray(), Occurrence);

    public new IBooleanOperation GroupedAnd(IEnumerable<string> fields, params IExamineValue[] query)
        => this.GroupedAndInternal(fields.ToArray(), query, Occurrence);

    public new IBooleanOperation GroupedOr(IEnumerable<string> fields, params string[] query)
        => this.GroupedOrInternal(fields.ToArray(), query.Select(f => new ExamineValue(Examineness.Explicit, f)).Cast<IExamineValue>().ToArray(), Occurrence);

    public new IBooleanOperation GroupedOr(IEnumerable<string> fields, params IExamineValue[] query)
        => this.GroupedOrInternal(fields.ToArray(), query, Occurrence);

    public new IBooleanOperation GroupedNot(IEnumerable<string> fields, params string[] query)
        => this.GroupedNotInternal(fields.ToArray(), query.Select(f => new ExamineValue(Examineness.Explicit, f)).Cast<IExamineValue>().ToArray());

    public new IBooleanOperation GroupedNot(IEnumerable<string> fields, params IExamineValue[] query)
        => this.GroupedNotInternal(fields.ToArray(), query);
    private ElasticSearchBooleanOperation OrderByInternal(bool descending, params SortableField[] fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        foreach (var f in fields)
        {
            var fieldName = f.FieldName;

            SortFieldType defaultSort;

            switch (f.SortType)
            {
                case SortType.Score:
                    defaultSort = SortFieldType.SCORE;
                    break;
                case SortType.DocumentOrder:
                    defaultSort = SortFieldType.DOC;
                    break;
                case SortType.String:
                    defaultSort = SortFieldType.STRING_VAL;
                    break;
                case SortType.Int:
                    defaultSort = SortFieldType.INT32;
                    break;
                case SortType.Float:
                    defaultSort = SortFieldType.DOUBLE;
                    break;
                case SortType.Long:
                    defaultSort = SortFieldType.INT64;
                    break;
                case SortType.Double:
                    defaultSort = SortFieldType.DOUBLE;
                    break;

                default:
                    throw new InvalidOperationException("Unknown sort type");
            }

            _sortFields.Add(new SortField(fieldName, defaultSort, descending));
        }

        return new ElasticSearchBooleanOperation(this);
    }

    public IOrdering OrderBy(params SortableField[] fields) => OrderByInternal(false, fields);

    public IOrdering OrderByDescending(params SortableField[] fields) => OrderByInternal(true, fields);

    #region examineprivateorinternalmethods

    protected internal new LuceneBooleanOperationBase IdInternal(
        string id,
        Occur occurrence)
    {
        ArgumentNullException.ThrowIfNull(id);
        this.Query.Add(queryParser.GetFieldQueryInternal("__NodeId", id), occurrence);
        return this.CreateOp();
    }
    public ElasticSearchBooleanOperation ManagedQueryInternal(string query, string[] fields = null)
    {
        Query.Add(new LateBoundQuery(() =>
        {
            //if no fields are specified then use all fields
            fields = fields ?? AllFields;


            //Strangely we need an inner and outer query. If we don't do this then the lucene syntax returned is incorrect
            //since it doesn't wrap in parenthesis properly. I'm unsure if this is a lucene issue (assume so) since that is what
            //is producing the resulting lucene string syntax. It might not be needed internally within Lucene since it's an object
            //so it might be the ToString() that is the issue.
            var outer = new BooleanQuery();
            var inner = new BooleanQuery();
            var fielddefintion =
                searcher.AllProperties.Where(x => x.Value.Type == "text").Select(x => x.Key.Name);
            foreach (var field in fielddefintion)
            {
                var q = FullTextType.GenerateQuery(field, query, PatternAnalyzer.DEFAULT_ANALYZER);
                if (q != null)
                {
                    //CriteriaContext.ManagedQueries.Add(new KeyValuePair<IIndexFieldValueType, Query>(type, q));
                    inner.Add(q, Occur.SHOULD);
                }

            }

            outer.Add(inner, Occur.SHOULD);

            return outer;
        }), Occurrence);


        return new ElasticSearchBooleanOperation(this);
    }

    protected internal new LuceneBooleanOperationBase FieldInternal(string fieldName, IExamineValue fieldValue, Occur occurrence)
    {
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentNullException.ThrowIfNull(fieldValue);
        return FieldInternal(fieldName, fieldValue, occurrence, true);
    }

    private LuceneBooleanOperationBase FieldInternal(string fieldName, IExamineValue fieldValue, Occur occurrence, bool useQueryParser)
    {
        Query queryToAdd = GetFieldInternalQuery(fieldName, fieldValue, useQueryParser);

        if (queryToAdd != null)
            Query.Add(queryToAdd, occurrence);

        return CreateOp();
    }

    protected internal new LuceneBooleanOperationBase GroupedAndInternal(string[] fields, IExamineValue[] fieldVals, Occur occurrence)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(fieldVals);

        //if there's only 1 query text we want to build up a string like this:
        //(+field1:query +field2:query +field3:query)
        //but Lucene will bork if you provide an array of length 1 (which is != to the field length)

        Query.Add(GetMultiFieldQuery(fields, fieldVals, Occur.MUST), occurrence);

        return CreateOp();
    }

    protected internal new LuceneBooleanOperationBase GroupedNotInternal(string[] fields, IExamineValue[] fieldVals)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(fieldVals);

        //if there's only 1 query text we want to build up a string like this:
        //(!field1:query !field2:query !field3:query)
        //but Lucene will bork if you provide an array of length 1 (which is != to the field length)

        Query.Add(GetMultiFieldQuery(fields, fieldVals, Occur.MUST_NOT, true),
            //NOTE: This is important because we cannot prefix a + to a group of NOT's, that doesn't work.
            // for example, it cannot be:  +(-id:1 -id:2 -id:3)
            // it just needs to be          (-id:1 -id:2 -id:3)
            Occur.SHOULD);

        return CreateOp();
    }

    protected internal new LuceneBooleanOperationBase GroupedOrInternal(string[] fields, IExamineValue[] fieldVals, Occur occurrence)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(fieldVals);

        //if there's only 1 query text we want to build up a string like this:
        //(field1:query field2:query field3:query)
        //but Lucene will bork if you provide an array of length 1 (which is != to the field length)

        Query.Add(GetMultiFieldQuery(fields, fieldVals, Occur.SHOULD, true), occurrence);

        return CreateOp();
    }

    private BooleanQuery GetMultiFieldQuery(
        IReadOnlyList<string> fields,
        IExamineValue[] fieldVals,
        Occur occurrence,
        bool matchAllCombinations = false)
    {

        var qry = new BooleanQuery();
        if (matchAllCombinations)
        {
            foreach (var f in fields)
            {
                foreach (var val in fieldVals)
                {
                    var q = GetFieldInternalQuery(f, val, true);
                    if (q != null)
                    {
                        qry.Add(q, occurrence);
                    }
                }
            }
        }
        else
        {
            var queryVals = new IExamineValue[fields.Count];
            if (fieldVals.Length == 1)
            {
                for (int i = 0; i < queryVals.Length; i++)
                    queryVals[i] = fieldVals[0];
            }
            else
            {
                queryVals = fieldVals;
            }

            for (int i = 0; i < fields.Count; i++)
            {
                var q = GetFieldInternalQuery(fields[i], queryVals[i], true);
                if (q != null)
                {
                    qry.Add(q, occurrence);
                }
            }
        }

        return qry;
    }
    private Query GetFieldInternalQuery(string fieldName, IExamineValue fieldValue, bool useQueryParser)
    {
        Query queryToAdd;

        switch (fieldValue.Examineness)
        {
            case Examineness.Fuzzy:
                if (useQueryParser)
                {
                    queryToAdd = queryParser.GetFuzzyQueryInternal(fieldName, fieldValue.Value.Replace("-", "\\-"), fieldValue.Level);
                }
                else
                {
                    //REFERENCE: http://lucene.apache.org/java/2_4_0/queryparsersyntax.html#Fuzzy%20Searches
                    var proxQuery = fieldName + ":" + fieldValue.Value.Replace("-", "\\-") + "~" + Convert.ToInt32(fieldValue.Level);
                    queryToAdd = ParseRawQuery(proxQuery);
                }
                break;
            case Examineness.SimpleWildcard:
            case Examineness.ComplexWildcard:
                if (useQueryParser)
                {
                    queryToAdd = queryParser.GetWildcardQueryInternal(fieldName, fieldValue.Value.Replace("-", "\\-"));
                }
                else
                {
                    //this will already have a * or a . suffixed based on the extension methods
                    //REFERENCE: http://lucene.apache.org/java/2_4_0/queryparsersyntax.html#Wildcard%20Searches
                    var proxQuery = fieldName + ":" + fieldValue.Value.Replace("-", "\\-");
                    queryToAdd = ParseRawQuery(proxQuery);
                }
                break;
            case Examineness.Boosted:
                if (useQueryParser)
                {
                    queryToAdd = queryParser.GetFieldQueryInternal(fieldName, fieldValue.Value.Replace("-", "\\-"));
                    queryToAdd.Boost = fieldValue.Level;
                }
                else
                {
                    //REFERENCE: http://lucene.apache.org/java/2_4_0/queryparsersyntax.html#Boosting%20a%20Term
                    var proxQuery = fieldName + ":\"" + fieldValue.Value.Replace("-", "\\-") + "\"^" + Convert.ToInt32(fieldValue.Level);
                    queryToAdd = ParseRawQuery(proxQuery);
                }
                break;
            case Examineness.Proximity:

                //This is how you are supposed to do this based on this doc here:
                //http://lucene.apache.org/java/2_4_1/api/org/apache/lucene/search/spans/package-summary.html#package_description
                //but i think that lucene.net has an issue with it's internal parser since it parses to a very strange query
                //we'll just manually make it instead below

                //var spans = new List<SpanQuery>();
                //foreach (var s in fieldValue.Value.Split(' '))
                //{
                //    spans.Add(new SpanTermQuery(new Term(fieldName, s)));
                //}
                //queryToAdd = new SpanNearQuery(spans.ToArray(), Convert.ToInt32(fieldValue.Level), true);

                var qry = fieldName + ":\"" + fieldValue.Value.Replace("-", "\\-") + "\"~" + Convert.ToInt32(fieldValue.Level);
                if (useQueryParser)
                {
                    queryToAdd = queryParser.Parse(qry);
                }
                else
                {
                    queryToAdd = ParseRawQuery(qry);
                }
                break;
            case Examineness.Escaped:

                //This uses the KeywordAnalyzer to parse the 'phrase'
                var stdQuery = fieldName + ":" + fieldValue.Value.Replace("-", "\\-");

                //NOTE: We used to just use this but it's more accurate/exact with the below usage of phrase query
                //queryToAdd = ParseRawQuery(stdQuery);

                //This uses the PhraseQuery to parse the phrase, the results seem identical
                queryToAdd = ParseRawQuery(fieldName, fieldValue.Value.Replace("-", "\\-"));

                break;
            case Examineness.Explicit:
            default:
                if (useQueryParser)
                {
                    queryToAdd = queryParser.GetFieldQueryInternal(fieldName, fieldValue.Value.Replace("-", "\\-"));
                }
                else
                {
                    //standard query
                    var proxQuery = fieldName + ":" + fieldValue.Value.Replace("-", "\\-");
                    queryToAdd = ParseRawQuery(proxQuery);
                }
                break;
        }
        return queryToAdd;
    }
    private Query ParseRawQuery(string rawQuery)
    {
        var parser = new QueryParser(_luceneVersion, "", new KeywordAnalyzer());
        return parser.Parse(rawQuery);
    }


    protected virtual Query ParseRawQuery(string field, string txt)
    {
        var phraseQuery = new PhraseQuery
        {
            Slop = 0
        };
        foreach (var val in txt.Split(_separator, StringSplitOptions.RemoveEmptyEntries))
        {
            phraseQuery.Add(new Term(field, val));
        }
        return phraseQuery;
    }

    #endregion
    private static readonly HashSet<string> _emptyHashSet = new HashSet<string>();
    private static readonly char[] _separator =
    [
        ' '
    ];

    public IOrdering SelectFieldsInternal(ISet<string> fieldNames)
    {
        throw new NotImplementedException();
    }

    public IOrdering SelectFieldInternal(string fieldName)
    {
        throw new NotImplementedException();
    }

    public IOrdering SelectAllFieldsInternal()
    {
        throw new NotImplementedException();
    }
}
