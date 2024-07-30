using Bielu.Examine.Core.Models;
using Examine;
using Examine.Lucene.Search;
using Examine.Search;
using Lucene.Net.Analysis;

namespace Bielu.Examine.Core.Services;

public interface IBieluExamineSearcher : ISearcher
{
    string[] AllFields { get; }
    IEnumerable<ExamineProperty> AllProperties { get;}

    IQuery CreateQuery(string category, BooleanOperation defaultOperation, Analyzer? luceneAnalyzer, LuceneSearchOptions? searchOptions);
}
