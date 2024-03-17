# Azure Search
## Installation
### Install provider
Please install the package from nuget:
```bash
dotnet add package Bielu.Examine.AzureSearch
```
Disclaimer: This package is not yet available on nuget, but it will be available soon. For testing Please build it from source after making sure that it is tagged as packable in csproj file.
### Add registration
After installation, you need to add registration of the package to your `Program.cs` file:
1. First find
```c#
  .AddBieluExamineForUmbraco()
```
2. Extend it by Fluent Configuration in following way
```c#
    .AddBieluExamineForUmbraco(bieluExamineConfigurator =>
    {
        bieluExamineConfigurator.AddAzureSearchServices();
    })
```
## Default Recommended configuration
### For Local development
Please use following configuration for local development
```json
{
  "bielu": {
    "Examine": {
      "Enabled": true,
      "AzureSearch": {
        "DevMode": true,
        "DefaultIndexConfiguration": {
          "Name": "ExternalIndex",
          "ConnectionString": "http://localhost:9200",
          "AuthenticationType": "None"
        },
        "IndexConfigurations": [
          {
            "Name": "ExternalIndex"
          },
          {
            "Name": "InternalIndex"
          },
          {
            "Name": "MembersIndex"
          },
          {
            "Name": "DeliveryApiContentIndex"
          }
        ]
      }
    }
  }
}
```
## Configuration schema
This schema correspond to configuration for elasticsearch. It is possible to configure elasticsearch for each index separately or use shared configuration for all indexes.
```json
```
{src="../../../src/Bielu.Examine.AzureSearch/schema/appsettings-schema.BieluExamineAzureSearchOptions.json" }
