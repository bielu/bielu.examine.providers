using System.Globalization;
using Lucene.Net.Documents;
using Lucene.Net.Index;

namespace Bielu.Examine.Elasticsearch.Model;

public class ElasticDocument : Dictionary<string, object>
{
    public Field GetField(string fieldName)
    {
        if (ContainsKey(fieldName))
        {
            return new Field(fieldName,Convert.ToString(this[fieldName],CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.ANALYZED);
        }

        return null;

    }

    public IList<IIndexableField> GetFields()
    {
        var results = new List<IIndexableField>();

        foreach(var f in this)
        {
            results.Add(new Field(f.Key, Convert.ToString(f.Value,CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.ANALYZED));
        }

        return results;
    }

    public void Add(Field field)
    {
        this[field.Name] = field.GetStringValue(CultureInfo.InvariantCulture);
    }
}
