namespace Bielu.Examine.Core.Models;

public class ExamineIndexState
{
    public string IndexAlias { get; set; }
    public string? CurrentIndexName { get; set; }
    public string? CurrentTemporaryIndexName { get; set; }
    public string TempIndexAlias { get; set; }
    public string? IndexName { get; set; }
    public bool Exist { get; set; }
    public bool CreatingNewIndex { get; set; }
    public bool Reindexing { get; set; }

}
