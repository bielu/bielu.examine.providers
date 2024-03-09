using Bielu.Examine.Elasticsearch.Configuration;
using Bielu.Examine.Elasticsearch.Model;
using Microsoft.Extensions.Options;

namespace Bielu.Examine.Elasticsearch.Services;

public class IndexStateService(IOptionsMonitor<BieluExamineElasticOptions> examineElasticOptions) : IIndexStateService
{
    private Dictionary<string,ExamineIndexState> _indexStates = new Dictionary<string,ExamineIndexState>();
    public ExamineIndexState GetIndexState(string indexName)
    {
        if(_indexStates.TryGetValue(indexName, out var state))
        {
            return state;
        }
        var configuration = examineElasticOptions.CurrentValue.IndexConfigurations.FirstOrDefault(x => x.Name.Equals(indexName, StringComparison.OrdinalIgnoreCase));
        state = new ExamineIndexState();
        state.IndexName = indexName;
        state.IndexAlias = $"{configuration.Prefix.ToLowerInvariant()}_{indexName.ToLowerInvariant()}";
        state.TempIndexAliast = $"{configuration.Prefix.ToLowerInvariant()}_temp_{indexName.ToLowerInvariant()}";
        _indexStates[indexName] = state;
        return state;
    }
}
