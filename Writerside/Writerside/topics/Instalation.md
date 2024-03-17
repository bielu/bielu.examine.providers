# Installation

## Prerequisites
- Umbraco 13.2.0
- Examine 3.2
## Installation steps
Please install the package from nuget:
```bash
dotnet add package Bielu.Examine.Umbraco
```
After installation, you need to add registration of the package to your `Program.cs` file:
1. First find
```c#
    .AddComposers()
```
2. Add registration afterward, like this:
```c#
    .AddBieluExamineForUmbraco()
```
warning: It is important to add registration after `AddComposers` method, because it will register all necessary dependencies for you.
3. Go to specific provider and follow instructions from there. List of Providers is available [here](Providers.md).