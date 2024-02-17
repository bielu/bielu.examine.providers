using Bielu.Examine.Core.Extensions;
using Bielu.Examine.Elasticsearch.Umbraco.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Bielu.Examine.Elasticsearch.Umbraco.Composer;

public class UmbracoElasticsearchComposer: IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddCoreServices();
        builder.AddElasticIndexes();
    }
}
