using Examine;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Infrastructure.Examine;

namespace Bielu.Examine.Elasticsearch.Umbraco.Services;

public class ElasticSearchBackOfficeExamineSearcher : IBackOfficeExamineSearcher
{

    public IEnumerable<ISearchResult> Search(string query, UmbracoEntityTypes entityType, int pageSize, long pageIndex, out long totalFound, string? searchFrom = null, bool ignoreUserStartNodes = false) => throw new NotImplementedException();
}
