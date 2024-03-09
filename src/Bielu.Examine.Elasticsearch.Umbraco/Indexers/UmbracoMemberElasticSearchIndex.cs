using Bielu.Examine.Elasticsearch.Configuration;
using Bielu.Examine.Elasticsearch.Services;
using Examine;
using Examine.Lucene;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;

namespace Bielu.Examine.Elasticsearch.Umbraco.Indexers;

public class UmbracoMemberElasticSearchIndex(string? name, ILoggerFactory loggerFactory,  IRuntime runtime, ILogger<ElasticSearchUmbracoIndex> logger, IElasticsearchService elasticSearchService, IIndexStateService stateService,  IOptionsMonitor<LuceneDirectoryIndexOptions> indexOptions, IOptionsMonitor<BieluExamineElasticOptions> examineElasticOptions) : ElasticSearchUmbracoIndex(name, loggerFactory,runtime, logger,elasticSearchService,stateService, indexOptions, examineElasticOptions),IUmbracoMemberIndex
{

}
