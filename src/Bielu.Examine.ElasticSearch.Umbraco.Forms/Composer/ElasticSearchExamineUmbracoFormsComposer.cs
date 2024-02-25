using Bielu.Examine.Elasticsearch.Umbraco.DependencyInjection;
using Bielu.Examine.ElasticSearch.Umbraco.Form.Indexer;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Bielu.Examine.ElasticSearch.Umbraco.PDF.Composer;
public class ElasticSearchExamineUmbracoFormsComposer : IComposer
{

    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddExamineElasticSearchIndex<UmbracoFormsElasticIndex>(global::Umbraco.Forms.Core.Constants.ExamineIndex.RecordIndexName);
    }
}
