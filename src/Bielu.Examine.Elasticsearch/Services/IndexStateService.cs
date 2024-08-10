using Bielu.Examine.Core.Models;
using Bielu.Examine.Core.Services;
using Bielu.Examine.Elasticsearch.Configuration;
using Bielu.Examine.Elasticsearch.Model;
using Microsoft.Extensions.Options;

namespace Bielu.Examine.Elasticsearch.Services;

public class IndexStateService(IOptionsMonitor<BieluExamineElasticOptions> examineElasticOptions) : IIndexStateService
{
    private Dictionary<string, ExamineIndexState> _indexStates = [];

    public ExamineIndexState GetIndexState(string indexName, ISearchService searchService)
    {
        if (_indexStates.TryGetValue(indexName, out var state))
        {
            return state;
        }
        var elasticConfig = examineElasticOptions.CurrentValue;
        var configuration = elasticConfig.IndexConfigurations.FirstOrDefault(x => x.Name.Equals(indexName, StringComparison.OrdinalIgnoreCase));
        state = new ExamineIndexState
        {
            IndexName = indexName,
            Analyzer = configuration?.Analyzer ?? elasticConfig.DefaultIndexConfiguration?.Analyzer
        };
        var prefix = (configuration?.Prefix?.ToLowerInvariant() ?? elasticConfig.DefaultIndexConfiguration?.Prefix)?.ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            prefix += "_";
        }
        state.IndexAlias = $"{prefix}{indexName.ToLowerInvariant()}";
        state.TempIndexAlias = $"{prefix}temp_{indexName.ToLowerInvariant()}";
        _indexStates[indexName] = state;
        state.Exist = searchService?.IndexExists(indexName) ?? false;
        return state;
    }
}
