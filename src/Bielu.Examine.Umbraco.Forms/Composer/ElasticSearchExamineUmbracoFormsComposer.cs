using Bielu.Examine.Core.Configuration;
using Bielu.Examine.Core.Services;
using Bielu.Examine.ElasticSearch.Umbraco.Form.Indexer;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using bielu.Examine.Umbraco.Extensions;
namespace Bielu.Examine.ElasticSearch.Umbraco.Form.Composer;
public static class BieluExamineConfiguratorExtensions
{

    public static BieluExamineConfigurator AddFormProvider(this BieluExamineConfigurator builder)
    {
        builder.ServiceCollection.AddBieluExamineIndex<BieluExamineUmbracoFormsIndex>(global::Umbraco.Forms.Core.Constants.ExamineIndex.RecordIndexName);
        return builder;
    }
}
