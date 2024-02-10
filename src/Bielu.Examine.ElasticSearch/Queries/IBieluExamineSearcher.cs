using Examine;

namespace Bielu.Examine.Core.Services;

public interface IBieluExamineSearcher : ISearcher
{

    IEnumerable<string> AllProperties { get; set; }
}
