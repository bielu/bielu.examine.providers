using Bielu.Examine.AzureSearch.Extensions;
using Bielu.Examine.Elasticsearch.Extensions;
using Bielu.Examine.ElasticSearch.Umbraco.Form.Composer;
using bielu.Examine.Umbraco.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
    .AddBieluExamineForUmbraco(bieluExamineConfigurator =>
    {
        bieluExamineConfigurator.AddFormProvider();
      //  bieluExamineConfigurator.AddElasticsearchServices();
      bieluExamineConfigurator.AddAzureSearchServices();
    })
    .Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();


app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseInstallerEndpoints();
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
