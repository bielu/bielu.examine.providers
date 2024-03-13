using Bielu.Examine.Core.Services;
using Examine;
using Examine.Lucene;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace bielu.Examine.Umbraco.Extensions;

public static class ServiceCollectionExtensions
{
    public static IUmbracoBuilder AddUmbraco(this IUmbracoBuilder builder)
    {

    }
    public static IServiceCollection AddBieluExamineIndex<TIndex, TExamineOptions, TIndexStateService,TIndexService>(this IServiceCollection serviceCollection, string name) where TIndex : class, IBieluExamineIndex
    {
        return serviceCollection.AddSingleton<IIndex>(services =>
        {
            IRuntime runtime = services.GetRequiredService<IRuntime>();
            ILogger<IBieluExamineIndex> logger = services.GetRequiredService<ILogger<IBieluExamineIndex>>();
            ILoggerFactory loggerFactory = services.GetRequiredService<ILoggerFactory>();
            TIndexStateService stateService = services.GetRequiredService<TIndexStateService>();
            TIndexService indexsearchService = services.GetRequiredService<TIndexService>();

            IOptionsMonitor<LuceneDirectoryIndexOptions> indexOptions = services.GetRequiredService<IOptionsMonitor<LuceneDirectoryIndexOptions>>();
            IOptionsMonitor<TExamineOptions> examineElasticOptions = services.GetRequiredService<IOptionsMonitor<TExamineOptions>>();
            return (TIndex)ActivatorUtilities.CreateInstance(services,typeof(TIndex) ,(object)name, (object)loggerFactory, runtime, logger, indexsearchService, stateService, indexOptions, examineElasticOptions);
        });
    }
}
