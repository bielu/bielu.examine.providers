using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Examine.Lucene.Analyzers;
using Examine.Lucene;
using Examine;
using Lucene.Net.Analysis;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Forms.Examine.Indexes;

namespace Bielu.Examine.ElasticSearch.Umbraco.Form.Configuration;
public class ConfigureUmbracoFormsIndexOptions :
    IConfigureNamedOptions<LuceneDirectoryIndexOptions>,
    IConfigureOptions<LuceneDirectoryIndexOptions>
{
    private readonly IUmbracoIndexConfig _umbracoIndexConfig;

    public ConfigureUmbracoFormsIndexOptions(IUmbracoIndexConfig umbracoIndexConfig)
    {
        this._umbracoIndexConfig = umbracoIndexConfig;
    }

    public void Configure(LuceneDirectoryIndexOptions options)
    {
        throw new NotSupportedException("This is never called and is just part of the interface");
    }

    public void Configure(string? name, LuceneDirectoryIndexOptions options)
    {
        if (!(name == "UmbracoFormsRecordsIndex"))
            return;
        options.Analyzer = (Analyzer)new CultureInvariantWhitespaceAnalyzer();
        options.Validator = (IValueSetValidator)new RecordValueSetValidator();
        options.FieldDefinitions = new FieldDefinitionCollection(new FieldDefinition[] { });
    }
}
