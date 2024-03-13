using Bielu.Examine.Core.Services;
using Bielu.Examine.Elasticsearch.Configuration;
using Bielu.Examine.Elasticsearch.Model;
using Bielu.Examine.Elasticsearch.Services;
using bielu.Examine.Umbraco.Extensions;
using bielu.Examine.Umbraco.Indexers.Indexers;
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
        services.AddBieluExamineIndex<UmbracoContentElasticsearchIndex,BieluExamineElasticOptions,IIndexStateService,IElasticsearchService>(global::Umbraco.Cms.Core.Constants.UmbracoIndexes
            .InternalIndexName);
        services.AddBieluExamineIndex<UmbracoContentElasticsearchIndex,BieluExamineElasticOptions,IIndexStateService,IElasticsearchService>(global::Umbraco.Cms.Core.Constants.UmbracoIndexes
            .ExternalIndexName);
        services.AddBieluExamineIndex<UmbracoContentElasticsearchIndex,BieluExamineElasticOptions,IIndexStateService,IElasticsearchService>(global::Umbraco.Cms.Core.Constants.UmbracoIndexes
            .MembersIndexName);
        services.AddBieluExamineIndex<UmbracoDeliveryApiContentElasticSearchIndex,BieluExamineElasticOptions,IIndexStateService,IElasticsearchService>(global::Umbraco.Cms.Core.Constants.UmbracoIndexes
            .DeliveryApiContentIndexName);
        services.AddSingleton<IExamineManager, ExamineManager<IBieluExamineIndex>>();
        return umbracoBuilder;
    }

}
