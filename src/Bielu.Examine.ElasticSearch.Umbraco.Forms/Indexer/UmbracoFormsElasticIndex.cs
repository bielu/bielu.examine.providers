using Bielu.Examine.Elasticsearch.Configuration;
using Bielu.Examine.Elasticsearch.Services;
using Bielu.Examine.Elasticsearch.Umbraco.Indexers;
using Examine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Services;
using Umbraco.Forms.Examine.Indexes;

namespace Bielu.Examine.ElasticSearch.Umbraco.Form.Indexer;

public class UmbracoFormsElasticIndex(string? name, ILoggerFactory loggerFactory, IElasticSearchClientFactory factory, IRuntime runtime, ILogger<ElasticSearchUmbracoIndex> logger, IOptionsMonitor<IndexOptions> indexOptions, IOptionsMonitor<BieluExamineElasticOptions> examineElasticOptions) : ElasticSearchUmbracoIndex(name, loggerFactory, factory, runtime, logger, indexOptions, examineElasticOptions),IUmbracoFormsRecordIndex
{

}
