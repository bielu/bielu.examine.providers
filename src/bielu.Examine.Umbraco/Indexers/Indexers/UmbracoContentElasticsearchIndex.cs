using Bielu.Examine.Core.Services;
using Examine.Lucene;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;

namespace bielu.Examine.Umbraco.Indexers.Indexers;

public class UmbracoContentElasticsearchIndex(string? name, ILoggerFactory loggerFactory, IRuntime runtime, ILogger<ElasticSearchUmbracoIndex> logger,ISearchService searchService, IIndexStateService stateService, IBieluSearchManager manager, IOptionsMonitor<LuceneDirectoryIndexOptions> indexOptions) : ElasticSearchUmbracoIndex(name, loggerFactory,runtime, logger,searchService,stateService,manager, indexOptions), IUmbracoContentIndex
{

}
