using Bielu.Examine.Core.Models;

namespace Bielu.Examine.Core.Services;

public interface IIndexStateService
{
    public ExamineIndexState GetIndexState(string indexName);
}
