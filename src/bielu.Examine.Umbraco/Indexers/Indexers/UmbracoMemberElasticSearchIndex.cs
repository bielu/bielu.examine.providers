using Bielu.Examine.Core.Services;
using Examine.Lucene;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;

namespace bielu.Examine.Umbraco.Indexers.Indexers;

public class UmbracoMemberElasticSearchIndex(string? name, ILoggerFactory loggerFactory,  IRuntime runtime, ILogger<ElasticSearchUmbracoIndex> logger, ISearchService elasticSearchService, IIndexStateService stateService,  IOptionsMonitor<LuceneDirectoryIndexOptions> indexOptions) : ElasticSearchUmbracoIndex(name, loggerFactory,runtime, logger,elasticSearchService,stateService, indexOptions),IUmbracoMemberIndex
{

}
