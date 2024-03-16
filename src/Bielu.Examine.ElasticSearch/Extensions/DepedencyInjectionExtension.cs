using Bielu.Examine.Core.Configuration;
using Bielu.Examine.Core.Services;
using Bielu.Examine.Elasticsearch.Configuration;
using Bielu.Examine.Elasticsearch.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Bielu.Examine.Elasticsearch.Extensions;

public static class DepedencyInjectionExtension
{
    public static BieluExamineConfigurator AddElasticsearchServices(this BieluExamineConfigurator configurator)
    {
        configurator.ServiceCollection.AddOptions<BieluExamineElasticOptions>().BindConfiguration(BieluExamineElasticOptions.SectionName);
        configurator.OptionsType = typeof(BieluExamineElasticOptions);
        configurator.ServiceCollection.AddSingleton<ISearchService, ElasticsearchService>();
        configurator.ServiceCollection.AddSingleton<IIndexStateService, IndexStateService>();
        configurator.ServiceCollection.AddSingleton<IBieluSearchManager, ElasticBieluSearchManager>();
        return configurator;
    }
}
