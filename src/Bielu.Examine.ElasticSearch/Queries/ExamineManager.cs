using Bielu.Examine.Core.Configuration;
using Examine;
using Microsoft.Extensions.Options;

namespace Bielu.Examine.Elasticsearch.Queries;

public class ExamineManager<T>(IEnumerable<IIndex> indexes, IEnumerable<ISearcher> searchers, IOptionsMonitor<BieluExamineOptions> optionsMonitor) : ExamineManager( FilterIndexes(optionsMonitor.CurrentValue,indexes), searchers)
{
    private static IEnumerable<IIndex> FilterIndexes(BieluExamineOptions options, IEnumerable<IIndex> indexes)
    {
        if(options.Enabled)
        {
            return indexes.Where(x=>!x.GetType().GetInterfaces().Contains(typeof(T)));
        }
        return indexes.Where(x=>x.GetType().GetInterfaces().Contains(typeof(T)));
    }
}
