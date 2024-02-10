using System.ComponentModel;
using Bielu.Examine.Elasticsearch2.Model;
using Examine;

namespace Bielu.Examine.Elasticsearch2.Events;

public class DocumentWritingEventArgs : CancelEventArgs
{
    /// <summary>
    /// Lucene.NET Document, including all previously added fields
    /// </summary>        
    public ElasticDocument Document { get; }

    /// <summary>
    /// Fields of the indexer
    /// </summary>
    public ValueSet ValueSet { get; }
        

    /// <summary>
    /// 
    /// </summary>
    /// <param name="valueSet"></param>
    /// <param name="d"></param>
    public DocumentWritingEventArgs(ValueSet valueSet, ElasticDocument d)
    {
        this.Document = d;
        this.ValueSet = valueSet;
    }
        
}