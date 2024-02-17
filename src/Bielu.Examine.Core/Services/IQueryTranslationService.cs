using Examine.Lucene.Indexing;

namespace Bielu.Examine.Core.Services;

public interface IQueryTranslationService<T, TProperty>
{

    IIndexFieldValueType FromSearchType(KeyValuePair<T, TProperty> propertyDescriptionPair);
}
