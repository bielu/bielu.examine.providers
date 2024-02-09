using Bielu.Examine.ElasticSearch.Indexers;
using BIelu.Examine.Umbraco.Indexers;
using Examine;
using Examine.Lucene;
using Examine.Lucene.Directories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Cms.Infrastructure.Examine.DependencyInjection;

namespace BIelu.Examine.Umbraco.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddExamineIndexes(this IUmbracoBuilder umbracoBuilder)
    {
        IServiceCollection services = umbracoBuilder.Services;

        services.AddSingleton<IBackOfficeExamineSearcher, BackOfficeExamineSearcher>();
        services.AddSingleton<IIndexDiagnosticsFactory, LuceneIndexDiagnosticsFactory>();

        services.AddExamine();

        // Create the indexes
        services

            .ConfigureOptions<ConfigureIndexOptions>();

        services.AddSingleton<IApplicationRoot, UmbracoApplicationRoot>();
        services.AddSingleton<ILockFactory, UmbracoLockFactory>();
        services.AddSingleton<ConfigurationEnabledDirectoryFactory>();
        services.AddExamineElasticSearchIndex<UmbracoContentElasticsearchIndex>(Constants.UmbracoIndexes
            .InternalIndexName);
        services.AddExamineElasticSearchIndex<UmbracoContentElasticsearchIndex>(Constants.UmbracoIndexes
            .ExternalIndexName);
        services.AddExamineElasticSearchIndex<UmbracoMemberElasticSearchIndex>(Constants.UmbracoIndexes
            .MembersIndexName);
        services.AddExamineElasticSearchIndex<UmbracoDeliveryApiContentElasticSearchIndex>(Constants.UmbracoIndexes
            .ExternalIndexName);
        return umbracoBuilder;
    }
    private static IServiceCollection AddExamineElasticSearchIndex<TIndex>(this IServiceCollection serviceCollection,string name) where TIndex : class, IElasticSearchExamineIndex
    {
        return serviceCollection.AddSingleton<TIndex>(services =>
        {
            IOptionsMonitor<LuceneDirectoryIndexOptions> requiredService = services.GetRequiredService<IOptionsMonitor<LuceneDirectoryIndexOptions>>();
            return (TIndex) ActivatorUtilities.CreateInstance<TIndex>(services, (object) name, (object) requiredService);
        });
    }
}
