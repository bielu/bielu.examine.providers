using Bielu.Examine.Elasticsearch.Configuration;
using Bielu.Examine.Elasticsearch.Services;
using Bielu.Examine.Elasticsearch.Umbraco.Indexers;
using Examine;
using Examine.Lucene;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Services;
using Umbraco.Forms.Examine.Indexes;

namespace Bielu.Examine.ElasticSearch.Umbraco.Form.Indexer;

public class UmbracoFormsElasticIndex(string? name,
    ILoggerFactory loggerFactory,
    IRuntime runtime,
    ILogger<ElasticSearchUmbracoIndex> logger,
    IElasticsearchService elasticSearchService,
    IIndexStateService stateService,
    IOptionsMonitor<LuceneDirectoryIndexOptions> indexOptions,
    IOptionsMonitor<BieluExamineElasticOptions> examineElasticOptions)
    : ElasticSearchUmbracoIndex(name, loggerFactory,runtime, logger,elasticSearchService,stateService, indexOptions, examineElasticOptions),IUmbracoFormsRecordIndex
{

}
