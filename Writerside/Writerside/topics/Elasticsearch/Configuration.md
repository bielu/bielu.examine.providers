# Elasticsearch
## Installation
### Install provider
Please install the package from nuget:
```bash
dotnet add package Bielu.Examine.Elasticsearch
```
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
        bieluExamineConfigurator.AddElasticsearchServices();
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
      "ElasticSearch": {
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
{src="../../../src/Bielu.Examine.Elasticsearch/schema/appsettings-schema.BieluExamineElasticOptions.json" }
### DevMode
This flag is responsible by enabling / disabling development mode for elasticsearch, it will enable debug mode on elasticsearch ([Docs](https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/debug-mode.html)).
### DefaultIndexConfiguration 
This is a default index configuration for elasticsearch. Please see OverrideClientConfiguration object description below. This configuration is used as shared configuration, in case when index configuration is not provided or index is not configured with Override Client Configuration flag.
### IndexConfigurations
This is a list of index configurations for elasticsearch. Each index configuration is a separate configuration for elasticsearch. It is possible to override default configuration for each index.

## Index Configuration
For each index configuration and default configuration it is possible to configure following options:
### Name
This is a name of index. This will be use also to store client information when overriding default configuration.
### OverrideClientConfiguration
This flag is responsible by enabling / disabling overriding default configuration for elasticsearch client. Default value is false.
### Analyzer
This is a name of analyzer for elasticsearch. Default value is standard.
### ConnectionString
This is a connection string for elasticsearch. It is required to be able to use elasticsearch.
### AuthenticationType
This is a type of authentication for elasticsearch. Default value is none. Currently Available options: None, Cloud, CloudApi.
### AuthenticationDetails
This is a details for authentication for elasticsearch. It is required to be able to use elasticsearch, when authentication type is not None.
#### AuthenticationDetails.Id
This is a username for elasticsearch. It is required to be able to use elasticsearch, when authentication type is Cloud or CloudApi.
#### AuthenticationDetails.Username
This is a username for elasticsearch. It is required to be able to use elasticsearch, when authentication type is Cloud.
#### AuthenticationDetails.Password
This is a password for elasticsearch. It is required to be able to use elasticsearch, when authentication type is Cloud.
#### AuthenticationDetails.ApiKey
This is a api key for elasticsearch. It is required to be able to use elasticsearch, when authentication type is CloudApi.