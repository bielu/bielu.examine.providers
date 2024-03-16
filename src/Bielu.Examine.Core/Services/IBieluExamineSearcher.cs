using Bielu.Examine.Core.Models;
using Examine;

namespace Bielu.Examine.Core.Services;

public interface IBieluExamineSearcher : ISearcher
{
    string[] AllFields { get; }
    IEnumerable<ExamineProperty> AllProperties { get;}
}
