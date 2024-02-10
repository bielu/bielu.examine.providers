using Bielu.Examine.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bielu.Examine.ElasticSearch.Extensions;

public static class DepedencyInjectionExtension
{
    public static void AddCoreServices(this IServiceCollection services)
    {
        services.AddOptions<BieluExamineOptions>().BindConfiguration(BieluExamineOptions.SectionName);
    }
}
