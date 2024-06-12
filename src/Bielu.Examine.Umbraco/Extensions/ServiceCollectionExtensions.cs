using Bielu.Examine.Core.Configuration;
using Bielu.Examine.Core.Extensions;
using Bielu.Examine.Core.Services;
using bielu.Examine.Umbraco.Indexers.Indexers;
using Examine;
using Examine.Lucene;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Extensions;

namespace bielu.Examine.Umbraco.Extensions;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddBieluExamineForUmbraco(this IUmbracoBuilder builder, Action<BieluExamineConfigurator?> configure)
    {
        builder.Services.AddCoreServices(configure);
        var config= BieluExamineConfiguration.Instance;
        if (config.FieldAnalyzerFieldMapping.TryGetValue("keyword", out var keyword))
        {
            keyword.Add(BieluExamineUmbracoIndex.IndexPathFieldName);
        }
        builder.Services.AddUnique<IIndexRebuilder, ElasticsearchExamineIndexRebuilder>();

        builder.Services.AddSingleton<IIndexDiagnosticsFactory, LuceneIndexDiagnosticsFactory>();
        builder.Services.AddBieluExamineIndex<BieluExamineUmbracoContentIndex>(global::Umbraco.Cms.Core.Constants.UmbracoIndexes
            .InternalIndexName);
        builder.Services.AddBieluExamineIndex<BieluExamineUmbracoContentIndex>(global::Umbraco.Cms.Core.Constants.UmbracoIndexes
            .ExternalIndexName);
        builder.Services.AddBieluExamineIndex<BieluExamineUmbracoMemberIndex>(global::Umbraco.Cms.Core.Constants.UmbracoIndexes
            .MembersIndexName);
        builder.Services.AddBieluExamineIndex<BieluExamineUmbracoDeliveryApiContentIndex>(global::Umbraco.Cms.Core.Constants.UmbracoIndexes
            .DeliveryApiContentIndexName);
        builder.Services.AddSingleton<IExamineManager, ExamineManager<IBieluExamineIndex>>();
        return builder;
    }
    public static IServiceCollection AddBieluExamineIndex<TIndex>(this IServiceCollection serviceCollection, string name) where TIndex : class, IBieluExamineIndex
    {
        return serviceCollection.AddSingleton<IIndex>(services =>
        {
            IRuntime runtime = services.GetRequiredService<IRuntime>();
            ILogger<IBieluExamineIndex> logger = services.GetRequiredService<ILogger<IBieluExamineIndex>>();
            ILoggerFactory loggerFactory = services.GetRequiredService<ILoggerFactory>();
            IIndexStateService stateService = services.GetRequiredService<IIndexStateService>();
            ISearchService indexsearchService = services.GetRequiredService<ISearchService>();
            IBieluSearchManager searchManager = services.GetRequiredService<IBieluSearchManager>();
            IOptionsMonitor<LuceneDirectoryIndexOptions> indexOptions = services.GetRequiredService<IOptionsMonitor<LuceneDirectoryIndexOptions>>();
            return (TIndex)ActivatorUtilities.CreateInstance(services,typeof(TIndex) ,(object)name, (object)loggerFactory, runtime, logger, indexsearchService, stateService,searchManager, indexOptions);
        });
    }
}
