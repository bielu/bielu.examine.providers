using Bielu.Examine.Elasticsearch.Configuration;
using Bielu.Examine.Elasticsearch.Indexers;
using Bielu.Examine.Elasticsearch.Model;
using Bielu.Examine.Elasticsearch.Queries;
using Bielu.Examine.Elasticsearch.Services;
using Bielu.Examine.Elasticsearch.Umbraco.Indexers;
using Examine;
using Examine.Lucene;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;

namespace Bielu.Examine.Elasticsearch.Umbraco.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddElasticIndexes(this IUmbracoBuilder umbracoBuilder)
    {
        IServiceCollection services = umbracoBuilder.Services;
        services.AddOptions<BieluExamineElasticOptions>().BindConfiguration(BieluExamineElasticOptions.SectionName);
        services.AddSingleton<IBackOfficeExamineSearcher, BackOfficeExamineSearcher>();
        services.AddSingleton<IIndexDiagnosticsFactory, LuceneIndexDiagnosticsFactory>();
        services.AddSingleton<IElasticSearchClientFactory, ElasticSearchClientFactory>();
        services.AddSingleton<IElasticsearchService, ElasticsearchService>();
        services.AddSingleton<IIndexStateService, IndexStateService>();
        services.AddExamineElasticSearchIndex<UmbracoContentElasticsearchIndex>(global::Umbraco.Cms.Core.Constants.UmbracoIndexes
            .InternalIndexName);
        services.AddExamineElasticSearchIndex<UmbracoContentElasticsearchIndex>(global::Umbraco.Cms.Core.Constants.UmbracoIndexes
            .ExternalIndexName);
        services.AddExamineElasticSearchIndex<UmbracoMemberElasticSearchIndex>(global::Umbraco.Cms.Core.Constants.UmbracoIndexes
            .MembersIndexName);
        services.AddExamineElasticSearchIndex<UmbracoDeliveryApiContentElasticSearchIndex>(global::Umbraco.Cms.Core.Constants.UmbracoIndexes
            .DeliveryApiContentIndexName);
        services.AddSingleton<IExamineManager, ExamineManager<IElasticSearchExamineIndex>>();
        return umbracoBuilder;
    }
    public static IServiceCollection AddExamineElasticSearchIndex<TIndex>(this IServiceCollection serviceCollection, string name) where TIndex : class, IElasticSearchExamineIndex
    {
        return serviceCollection.AddSingleton<IIndex>(services =>
        {
            IRuntime runtime = services.GetRequiredService<IRuntime>();
            ILogger<ElasticSearchUmbracoIndex> logger = services.GetRequiredService<ILogger<ElasticSearchUmbracoIndex>>();
            ILoggerFactory loggerFactory = services.GetRequiredService<ILoggerFactory>();
            IIndexStateService stateService = services.GetRequiredService<IIndexStateService>();
            IElasticsearchService elasticsearchService = services.GetRequiredService<IElasticsearchService>();

            IOptionsMonitor<LuceneDirectoryIndexOptions> indexOptions = services.GetRequiredService<IOptionsMonitor<LuceneDirectoryIndexOptions>>();
            IOptionsMonitor<BieluExamineElasticOptions> examineElasticOptions = services.GetRequiredService<IOptionsMonitor<BieluExamineElasticOptions>>();
            return (TIndex)ActivatorUtilities.CreateInstance<TIndex>(services, (object)name, (object)loggerFactory, runtime, logger, elasticsearchService, stateService, indexOptions, examineElasticOptions);
        });
    }
}
