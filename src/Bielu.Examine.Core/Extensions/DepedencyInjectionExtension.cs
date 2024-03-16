using Bielu.Examine.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bielu.Examine.Core.Extensions;

public static class DepedencyInjectionExtension
{
    public static void AddCoreServices(this IServiceCollection services, Action<BieluExamineConfigurator?> configure)
    {
        var configuration = BieluExamineConfiguration.Instance;
        var configurator = new BieluExamineConfigurator(configuration, services);
        configure(configurator);
        services.AddSingleton(configuration);
        services.AddOptions<BieluExamineOptions>().BindConfiguration(BieluExamineOptions.SectionName);
    }
}
