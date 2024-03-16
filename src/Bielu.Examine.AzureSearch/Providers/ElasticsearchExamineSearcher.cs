using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Bielu.Examine.Core.Models;
using Bielu.Examine.Core.Queries;
using Bielu.Examine.Core.Services;
using Bielu.Examine.Elasticsearch.Configuration;
using Bielu.Examine.Elasticsearch.Extensions;
using Bielu.Examine.Elasticsearch.Helpers;
using Bielu.Examine.Elasticsearch.Model;
using Bielu.Examine.Elasticsearch.Services;
using Examine;
using Examine.Lucene.Search;
using Examine.Search;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bielu.Examine.Elasticsearch.Providers;

public class AzureSearchExamineSearcher(string name, string? indexAlias, ILoggerFactory loggerFactory, ISearchService elasticsearchService) : BaseSearchProvider(name),IBieluExamineSearcher, IDisposable
{
    public string? IndexAlias => indexAlias;
    private readonly List<SortField> _sortFields = new List<SortField>();
    private string?[] _allFields;
    private IEnumerable<ExamineProperty>? _fieldsMapping;
    private bool? _exists;


    private static readonly string[]? _emptyFields = Array.Empty<string>();
    public bool IndexExists
    {
        get
        {
            if(_exists.HasValue)
            {
                return (bool)_exists;
            }
            _exists = elasticsearchService.IndexExists(name);
            return (bool)_exists;
        }
    }


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

    private BieluExamineSearchResults DoSearch(Query query, QueryOptions options)
    {
        return elasticsearchService.Search(name,options,query);

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
    public override ISearchResults Search(string searchText, QueryOptions options = null)
    {
        SearchOptions searchOptions = new SearchOptions()
        {
            IncludeTotalCount = true,
            Filter = "search.ismatch('" + searchText + "')", //todo: test this
            OrderBy = { "" },
            QueryType = SearchQueryType.Simple,
        };
       return elasticsearchService.Search(searchText,searchOptions);
    }
    public override IQuery CreateQuery(string category = null,
        BooleanOperation defaultOperation = BooleanOperation.And)
    {
        return elasticsearchService.CreateQuery(name, indexAlias, category, defaultOperation);
    }
 #pragma warning disable CA1816
    public void Dispose() => loggerFactory.Dispose();
 #pragma warning restore CA1816
}
