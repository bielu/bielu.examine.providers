using Bielu.Examine.Elasticsearch.Configuration;
using Bielu.Examine.Elasticsearch.Services;
using Bielu.Examine.Elasticsearch.Umbraco.DependencyInjection;
using Bielu.Examine.ElasticSearch.Umbraco.Form.Indexer;
using bielu.Examine.Umbraco.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Bielu.Examine.ElasticSearch.Umbraco.PDF.Composer;
public class ElasticSearchExamineUmbracoFormsComposer : IComposer
{

    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddBieluExamineIndex<UmbracoFormsElasticIndex,BieluExamineElasticOptions,IIndexStateService,IElasticsearchService>(global::Umbraco.Forms.Core.Constants.ExamineIndex.RecordIndexName);
    }
}
