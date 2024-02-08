using Examine.Lucene.Search;
using Lucene.Net.Analysis;
using Lucene.Net.Util;

namespace Bielu.Examine.ElasticSearch.Queries
{
    public class ElasticSearchQueryParser : CustomMultiFieldQueryParser 
    {

        public ElasticSearchQueryParser(LuceneVersion matchVersion, string[] fields, Analyzer analyzer) : base(matchVersion, fields, analyzer)
        {
        }
    }
}