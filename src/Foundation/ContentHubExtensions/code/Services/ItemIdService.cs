﻿using Sitecore.Data;
using Sitecore.Data.Items;
using Stylelabs.M.Sdk.Contracts.Base;
using System;
using System.Linq;
using Foundation.ContentHubExtensions.Utils;
using ItemUtil = Foundation.ContentHubExtensions.Utils.ItemUtil;

namespace Foundation.ContentHubExtensions.Services
{
    public class ItemIdService : IItemIdService
    {
        public static Item CmpConfigItem = ItemUtil.GetConfigItem();
        public static Item CmpIdProvidersItem = ItemUtil.GetIdProviderItem();

        public ID GenerateId(IEntity entity)
        {
            var mapping = CmpConfigItem.Children.FirstOrDefault(entityMapping =>
                entityMapping.TemplateID == Sitecore.Connector.CMP.Constants.EntityMappingTemplateId && string.Equals(
                    entity.DefinitionName,
                    entityMapping[Sitecore.Connector.CMP.Constants.EntityMappingEntityTypeSchemaFieldId],
                    StringComparison.Ordinal));

            if (mapping == null)
            {
                return ID.Null;
            }

            var idProvider = CmpIdProvidersItem.Children.FirstOrDefault(x =>
                string.Equals(mapping.ID.ToString(), x["Entity"]));

            if (idProvider == null)
            {
                return ID.Null;
            }

            var prefix = entity.DefinitionName;
            var idField = idProvider["Item Id Property"];
            var idVal = entity.GetPropertyValue(idField);
            var id = HashUtil.GetSitecoreGuid($"{prefix}-{idVal}");
            return new ID(id);
        }
    }
}