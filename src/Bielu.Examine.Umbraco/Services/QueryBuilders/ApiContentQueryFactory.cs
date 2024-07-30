using Bielu.Examine.Core.Services;
using Examine;
using Examine.Lucene.Search;
using Examine.Search;
using Lucene.Net.Analysis.Core;
using Umbraco.Cms.Api.Delivery.Services.QueryBuilders;
using Umbraco.Cms.Infrastructure.Examine;

namespace bielu.Examine.Umbraco.Services.QueryBuilders
{
    internal sealed class ApiContentQueryFactory : IApiContentQueryFactory
    {
        /// <inheritdoc/>
        public IQuery CreateApiContentQuery(IIndex index)
        {
            // Needed for enabling leading wildcards searches
            IBieluExamineSearcher searcher = index.Searcher as IBieluExamineSearcher ?? throw new InvalidOperationException($"Index searcher must be of type {nameof(IBieluExamineSearcher)}.");

            IQuery query = searcher.CreateQuery(
                IndexTypes.Content,
                BooleanOperation.And,
                new KeywordAnalyzer(),
                new LuceneSearchOptions() { AllowLeadingWildcard = true });

            return query;
        }
    }
}
