using System.ComponentModel;
using Bielu.Examine.Elasticsearch.Model;
using Examine;

namespace Bielu.Examine.Elasticsearch.Events;

public class DocumentWritingEventArgs : CancelEventArgs
{
    /// <summary>
    /// Lucene.NET Document, including all previously added fields
    /// </summary>        
    public BieluExamineDocument Document { get; }

    /// <summary>
    /// Fields of the indexer
    /// </summary>
    public ValueSet ValueSet { get; }
        

    /// <summary>
    /// 
    /// </summary>
    /// <param name="valueSet"></param>
    /// <param name="d"></param>
    public DocumentWritingEventArgs(ValueSet valueSet, BieluExamineDocument d)
    {
        this.Document = d;
        this.ValueSet = valueSet;
    }
        
}