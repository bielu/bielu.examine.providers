using Bielu.Examine.Core.Extensions;
using Bielu.Examine.Elasticsearch.Umbraco.DependencyInjection;
using Bielu.Examine.Elasticsearch.Umbraco.Services;
using bielu.Examine.Umbraco;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Extensions;

namespace Bielu.Examine.Elasticsearch.Umbraco.Composer;

public static class UmbracoElasticsearchExtensions
{
    public static IUmbracoBuilder AddElasticSearchExamineProvider(this IUmbracoBuilder builder)
    {
        builder.Services.AddCoreServices();
        builder.AddElasticIndexes();
        builder.Services.AddUnique<IIndexRebuilder, ElasticsearchExamineIndexRebuilder>();
        return builder;
    }
}
