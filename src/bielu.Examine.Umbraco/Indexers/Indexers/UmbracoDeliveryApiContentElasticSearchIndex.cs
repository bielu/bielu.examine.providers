using Bielu.Examine.Core.Services;
using Examine.Lucene;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Services;

namespace bielu.Examine.Umbraco.Indexers.Indexers;

public class UmbracoDeliveryApiContentElasticSearchIndex(string? name, ILoggerFactory loggerFactory,  IRuntime runtime, ILogger<ElasticSearchUmbracoIndex> logger, IElasticsearchService elasticSearchService, IIndexStateService stateService,  IOptionsMonitor<LuceneDirectoryIndexOptions> indexOptions, IOptionsMonitor<BieluExamineElasticOptions> examineElasticOptions) : ElasticSearchUmbracoIndex(name, loggerFactory,runtime, logger,elasticSearchService,stateService, indexOptions, examineElasticOptions)
{

}
