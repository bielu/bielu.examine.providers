using Bielu.Examine.Core.Models;
using Bielu.Examine.Core.Services;
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
        var elasticConfig = examineElasticOptions.CurrentValue;
        var configuration = elasticConfig.IndexConfigurations.FirstOrDefault(x => x.Name.Equals(indexName, StringComparison.OrdinalIgnoreCase));
        state = new ExamineIndexState();
        state.IndexName = indexName;

        state.Analyzer = configuration?.Analyzer;
        var prefix=(configuration?.Prefix?.ToLowerInvariant() ?? elasticConfig.DefaultIndexConfiguration?.Prefix)?.ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            prefix += "_";
        }
        state.IndexAlias = $"{prefix}{indexName.ToLowerInvariant()}";
        state.TempIndexAlias = $"{prefix}temp_{indexName.ToLowerInvariant()}";
        _indexStates[indexName] = state;
        return state;
    }
}
