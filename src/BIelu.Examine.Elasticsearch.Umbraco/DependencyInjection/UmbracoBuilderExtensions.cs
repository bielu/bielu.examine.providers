using Bielu.Examine.Core.Services;
using Bielu.Examine.Elasticsearch2.Configuration;
using Bielu.Examine.Elasticsearch2.Indexers;
using Bielu.Examine.Elasticsearch2.Services;
using BIelu.Examine.Umbraco.Indexers;
using Examine;
using Examine.Lucene;
using Examine.Lucene.Directories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Cms.Infrastructure.Examine.DependencyInjection;

namespace BIelu.Examine.Umbraco.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddElasticIndexes(this IUmbracoBuilder umbracoBuilder)
    {
        IServiceCollection services = umbracoBuilder.Services;
        services.AddOptions<BieluExamineElasticOptions>().BindConfiguration(BieluExamineElasticOptions.SectionName);
        services.AddSingleton<IBackOfficeExamineSearcher, BackOfficeExamineSearcher>();
        services.AddSingleton<IIndexDiagnosticsFactory, LuceneIndexDiagnosticsFactory>();
        services.AddSingleton<IElasticSearchClientFactory, ElasticSearchClientFactory>();
        services.AddExamineElasticSearchIndex<UmbracoContentElasticsearchIndex>(Constants.UmbracoIndexes
            .InternalIndexName);
        services.AddExamineElasticSearchIndex<UmbracoContentElasticsearchIndex>(Constants.UmbracoIndexes
            .ExternalIndexName);
        services.AddExamineElasticSearchIndex<UmbracoMemberElasticSearchIndex>(Constants.UmbracoIndexes
            .MembersIndexName);
        services.AddExamineElasticSearchIndex<UmbracoDeliveryApiContentElasticSearchIndex>(Constants.UmbracoIndexes
            .DeliveryApiContentIndexName);
        services.AddSingleton<IExamineManager, ExamineManager<IElasticSearchExamineIndex>>();
        return umbracoBuilder;
    }
    private static IServiceCollection AddExamineElasticSearchIndex<TIndex>(this IServiceCollection serviceCollection,string name) where TIndex : class, IElasticSearchExamineIndex
    {
        return serviceCollection.AddSingleton<IIndex>(services =>
        {
            IElasticSearchClientFactory factory = services.GetRequiredService<IElasticSearchClientFactory>();
            IRuntime runtime = services.GetRequiredService<IRuntime>();
            ILogger<ElasticSearchUmbracoIndex> logger = services.GetRequiredService<ILogger<ElasticSearchUmbracoIndex>>();
            IOptionsMonitor<IndexOptions> indexOptions = services.GetRequiredService<IOptionsMonitor<IndexOptions>>();
            IOptionsMonitor<BieluExamineElasticOptions> examineElasticOptions = services.GetRequiredService<IOptionsMonitor<BieluExamineElasticOptions>>();
            return (TIndex) ActivatorUtilities.CreateInstance<TIndex>(services, (object) name, (object) factory,runtime, logger,indexOptions,examineElasticOptions);
        });
    }
}
