using Bielu.Examine.ElasticSearch.Configuration;
using Bielu.Examine.ElasticSearch.Helpers;
using Bielu.Examine.ElasticSearch.Queries;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Ingest;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Examine;
using Examine.Search;
using Lucene.Net.Search;

namespace Bielu.Examine.ElasticSearch.Providers;

public class ElasticsearchExamineSearcher : BaseSearchProvider, IDisposable
{


    private readonly ExamineElasticOptions _connectionConfiguration;
        public readonly Lazy<ElasticsearchClient> _client;
        internal readonly List<SortField> _sortFields = new List<SortField>();
        private string[] _allFields;
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

        public ElasticsearchExamineSearcher(string name, string indexName) :
            base(name)
        {
            _indexName = name;
            _client = new Lazy<ElasticsearchClient>(()=>CreateElasticSearchClient(indexName));
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

                IEnumerable<PropertyName> keys = AllProperties.Keys;

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
                    _client.Value.Indices.GetMapping(new GetMappingRequest {IncludeTypeName = false});
                _fieldsMapping = response.GetMappingFor(indexesMappedToAlias[0]).Properties;
                return _fieldsMapping;
            }
        }

        public ISearchResults Search(string searchText, int maxResults = 500, int page = 1)
        {
            var query = new MultiMatchQuery
            {
                Query = searchText,
                Analyzer = "standard",
                Slop = 2,
                Type = TextQueryType.Phrase
            };

            return new ElasticSearchSearchResults(_client.Value, query, indexAlias, _sortFields, maxResults,
                maxResults * (page - 1));
        }


        public ISearchResults Search(QueryContainer queryContainer, int maxResults = 500)
        {
            return new ElasticSearchSearchResults(_client.Value, queryContainer, indexAlias, _sortFields, maxResults);
        }

        public ISearchResults Search(ISearchRequest searchRequest)
        {
            return new ElasticSearchSearchResults(_client.Value, searchRequest, indexAlias, _sortFields);
        }

        public ISearchResults Search(Func<SearchDescriptor<Document>, ISearchRequest> searchSelector)
        {
            return new ElasticSearchSearchResults(_client.Value, searchSelector, indexAlias, _sortFields);
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
            return new ElasticSearchSearchResults(_client.Value, query, indexAlias, _sortFields, maxResults);
        }

        public override IQuery CreateQuery(string category = null,
            BooleanOperation defaultOperation = BooleanOperation.And)
        {
            return new ElasticSearchQuery(this, category, AllFields, defaultOperation, indexAlias);
        }

        public void Dispose()
        {
        
        }
}