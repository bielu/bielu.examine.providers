using System.Reflection;
using Bielu.Examine.ElasticSearch.Configuration;
using bielu.SchemaGenerator.Build.Configuration;
using bielu.SchemaGenerator.Build.Services;
using CommandLine;
internal class Program
{
    static readonly IList<Assembly> Assemblies = new List<Assembly>()
    {
        typeof(ExamineElasticOptions).Assembly,
        typeof(UmbracoExamineElasticOptions).Assembly
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
        Console.WriteLine("Schema generator v {0}", typeof(SchemaGeneratorService).Assembly.GetName().Version.ToString());
      
            
            var schemaGenerator = new SchemaGeneratorService(new SchemaGenerator(), options);
            schemaGenerator.GenerateSchema(Assemblies);
   
        Console.WriteLine("Schema generator v {0}", typeof(SchemaGeneratorService).Assembly.GetName().Version.ToString());

    }
}