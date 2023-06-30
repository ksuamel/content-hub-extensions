# Overview
This repository serves as a way to extend and enhance the out of the box Content Hub CMP Connector for Sitecore.

Features:
- Deterministic Item Ids
- Remove Solr Dependency

# Installation

Installing and configuring the module is straight forward. 

1. Ensure you the the default CMP connector installed and working on your Sitecore solution
2. Deserialize the included Sitecore items
3. Deploy DLLs and configuration files
4. Configure new features


## Manual

### Deserializing the included Sitecore items

This module uses Sitecore Content Serialization module included in the Sitecore CLI. If you want to directly install these new templates into your instance, run the following commands at the root of the repo:

```
dotnet tool restore
dotnet sitecore login --authority <https://id.hostname.com> --cm <https://cm.hostname.com> --alow-write true
dotnet sitecore ser push
```

### Deploy DLLs and configuration files

To generate the DLLs required for this module, just build the solution. Inside of your bin, you will find the "Foundation.ContentHubExtensions.dll" file. Place this file inside of your Sitecore site's bin folder.

For the configuration path file, that can be found at /src/Foundation/ContentHubExtensions/App_Data/Include/Foundation/Foundation.ContentHubExtensions.config, place this file inside of your Sitecore site's Include folder. 

## Containers

[Coming Soon]

# Configuring features

Some features are required for the module to work, and other are optional.

Required:
- Id Providers

### Id Providers (Deterministic Ids)
For deterministic Ids to work properly, and effectively remove the Solr dependency in the process, we must configure Id Providers inside of Sitecore.

To configure Id Providers, simple create your entity mappings like normal, and then create an Id Provider for that entity mapping under ```/sitecore/system/modules/cmp/settings/id providers```

Example CMP Entity Mapping:
![Example CMP Entity Mapping](docs/images/entity_mapping.PNG)

Example CMP Id Provider:
![Example CMP Id Provider](docs/images/id_provider.PNG)

