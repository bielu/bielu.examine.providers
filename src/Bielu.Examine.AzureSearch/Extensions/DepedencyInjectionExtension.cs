using Bielu.Examine.AzureSearch.Services;
using Bielu.Examine.Core.Configuration;
using Bielu.Examine.Core.Services;
using Bielu.Examine.Elasticsearch.Configuration;
using Bielu.Examine.Elasticsearch.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Bielu.Examine.Elasticsearch.Extensions;

public static class DepedencyInjectionExtension
{
    public static BieluExamineConfigurator AddAzureSearchServices(this BieluExamineConfigurator configurator)
    {
        configurator.ServiceCollection.AddOptions<BieluExamineAzureSearchOptions>().BindConfiguration(BieluExamineAzureSearchOptions.SectionName);
        configurator.OptionsType = typeof(BieluExamineAzureSearchOptions);
        configurator.ServiceCollection.AddSingleton<ISearchService, AzureSearchService>();
        configurator.ServiceCollection.AddSingleton<IIndexStateService, IndexStateService>();
        configurator.ServiceCollection.AddSingleton<IBieluSearchManager, ElasticBieluSearchManager>();
        configurator.ServiceCollection.AddSingleton<IPropertyMappingService, PropertyMappingService>();
        configurator.ServiceCollection.AddSingleton<IAzureSearchClientFactory, AzureSearchClientFactory>();
        return configurator;
    }
}
