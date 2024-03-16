namespace Bielu.Examine.Core.Services;

public interface IBieluSearchManager
{
    public IBieluExamineSearcher GetSearcher(string? indexName);
}
