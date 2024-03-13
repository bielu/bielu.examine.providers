using Bielu.Examine.Core.Models;
using Examine;
using Examine.Search;

namespace Bielu.Examine.Core.Services;

public interface ISearchService
{
    public bool IndexExists(string examineIndexName);
    public IEnumerable<string>? GetCurrentIndexNames(string examineIndexName);
    public void EnsuredIndexExists(string examineIndexName, Func<object, object> fieldsMapping, bool overrideExisting = false);
    public void CreateIndex(string examineIndexName,Func<object, object> fieldsMapping);
    IEnumerable<ExamineProperty>? GetProperties(string examineIndexName);
    BieluExamineSearchResults Search(string examineIndexName,object searchDescriptor);
    void SwapTempIndex(string? examineIndexName);
    long IndexBatch(string? examineIndexName, IEnumerable<ValueSet> values);
    long DeleteBatch(string? examineIndexName, IEnumerable<string> itemIds);
    int GetDocumentCount(string? examineIndexName);
    bool HealthCheck(string? examineIndexNam);
    IQuery CreateQuery(string name, string? indexAlias, string category, BooleanOperation defaultOperation);
}
