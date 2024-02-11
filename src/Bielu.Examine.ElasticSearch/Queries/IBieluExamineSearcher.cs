using Examine;

namespace Bielu.Examine.Elasticsearch.Queries;

public interface IBieluExamineSearcher : ISearcher
{

    IEnumerable<string> AllProperties { get; set; }
}
