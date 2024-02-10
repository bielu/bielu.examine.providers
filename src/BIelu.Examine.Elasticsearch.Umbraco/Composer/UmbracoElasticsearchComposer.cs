using Bielu.Examine.ElasticSearch.Extensions;
using BIelu.Examine.Umbraco.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace BIelu.Examine.Umbraco.Composer;

public class UmbracoElasticsearchComposer: IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddCoreServices();
        builder.AddElasticIndexes();
    }
}
