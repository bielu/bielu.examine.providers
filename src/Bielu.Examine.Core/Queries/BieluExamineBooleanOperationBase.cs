using Examine;
using Examine.Lucene.Search;
using Examine.Search;
using Lucene.Net.Search;

namespace Bielu.Examine.Core.Queries;

public abstract class BieluExamineBooleanOperationBase(BieluExamineBaseQuery search):  LuceneBooleanOperationBase(search)
{
    #region IOrdering

    public override ISearchResults Execute(QueryOptions? options = null)
    {
        return   search.Execute(options);
    }

    public override IOrdering OrderBy(params SortableField[] fields) => search.OrderBy(fields);

    public override IOrdering OrderByDescending(params SortableField[] fields) => search.OrderByDescending(fields);

    #endregion
    #region Select Fields


    public override IOrdering SelectFields(ISet<string>? fieldNames) => search.SelectFieldsInternal(fieldNames);

    public override IOrdering SelectField(string fieldName) => search.SelectFieldInternal(fieldName);


    public override IOrdering SelectAllFields() => search.SelectAllFieldsInternal();

    #endregion
    public override string ToString() => search.ToString();

}
