using System.Reflection;
using Bielu.Examine.Core.Configuration;
using Bielu.Examine.Elasticsearch.Configuration;
using bielu.SchemaGenerator.Build.Configuration;
using bielu.SchemaGenerator.Build.Services;
using CommandLine;

namespace bielu.Umbraco.Cdn.SchemageGerator;

internal sealed class Program
{
    static readonly IList<Assembly> _assemblies = new List<Assembly>()
    {
        typeof(BieluExamineElasticOptions).Assembly,
        typeof(BieluExamineOptions).Assembly,
        typeof(BieluExamineAzureSearchOptions).Assembly,
    };

    public static async Task Main(string[] args)
    {
        try
        {
            await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(x=>Execute(x));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static async Task Execute(Options options)
    {
        Console.WriteLine("Schema generator v {0}", typeof(SchemaGeneratorService).Assembly.GetName().Version?.ToString());

        var schemaGenerator = new SchemaGeneratorService(new SchemaGenerator.Build.Services.SchemaGenerator(), options);
        schemaGenerator.GenerateSchema(_assemblies);

        Console.WriteLine("Schema generator v {0}", typeof(SchemaGeneratorService).Assembly.GetName().Version?.ToString());

    }
}
