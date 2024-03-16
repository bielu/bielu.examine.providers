using bielu.SchemaGenerator.Core.Attributes;

namespace Bielu.Examine.Core.Configuration;
public class BieluExamineConfiguration
{
    private BieluExamineConfiguration()
    {

    }
    private static BieluExamineConfiguration? _instance;
    public static BieluExamineConfiguration? Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = new BieluExamineConfiguration();
            }
            return _instance;
        }
    }
    public Dictionary<string,IList<string>> FieldAnalyzerFieldMapping { get; set; } = new Dictionary<string, IList<string>>();
    public bool EnableAliasing { get; set; } = true;

}
