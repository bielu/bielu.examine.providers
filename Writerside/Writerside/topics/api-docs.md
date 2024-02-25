# API Overview

<!-- This document provides an introduction into your API. -->

## Introduction

This project is reimplemented version of [Novicell.Examine.Elasticsearch](https://www.nuget.org/packages/Novicell.Examine.Elasticsearch/) package, to work with umbraco v13.

## Basic information
### Requirements

- Umbraco 13.1.0
- Examine 3.2
- Elasticsearch 8.12.0
## Recommendations
When using any of providers delivered with this package, it is recommended to not use Examine Api, but Search providers instead. This is because Examine Api is not aware of providers will limit functionality of your providers.
I would recommend writing own search abstractions and use them instead of Examine Api.
## Umbraco 8

If you are looking for v8 version of this package, please refer to [Novicell.Examine.Elasticsearch](https://www.nuget.org/packages/Novicell.Examine.Elasticsearch/).

