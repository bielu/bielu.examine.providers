using Bielu.Examine.Elasticsearch.Configuration;
using Bielu.Examine.Elasticsearch.Model;

namespace Bielu.Examine.Elasticsearch.Services;

public interface IIndexStateService
{
    public ExamineIndexState GetIndexState(string indexName);
}
