﻿using Foundation.ContentHubExtensions.Helpers;
using Foundation.ContentHubExtensions.Services;
using Sitecore.Abstractions;
using Sitecore.Connector.CMP;
using Sitecore.Connector.CMP.Pipelines.ImportEntity;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.SecurityModel;
using Stylelabs.M.Sdk.Contracts.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Foundation.ContentHubExtensions.Pipelines.cmp.importEntity
{
    public class EnsureRelation : ImportEntityProcessor
    {
        private static CmpSettings _settings;
        private readonly BaseFactory _factory;
        private readonly CustomCmpHelper _cmpHelper;
        private readonly IItemIdService _itemIdService;

        public EnsureRelation(
          BaseFactory factory,
          CustomCmpHelper cmpHelper,
          BaseLog logger,
          CmpSettings settings, 
          IItemIdService itemIdService)
          : base(logger, settings)
        {
            _factory = factory;
            _settings = settings;
            _cmpHelper = cmpHelper;
            _itemIdService = itemIdService;
        }

        public override void Process(ImportEntityPipelineArgs args, BaseLog logger)
        {
            Assert.IsNotNull(args.Item, "The item is null.");
            Assert.IsNotNull(args.Language, "The language is null.");
            using (new SecurityDisabler())
            {
                using (new LanguageSwitcher(args.Language))
                {
                    var flag = false;
                    try
                    {
                        args.Item.Editing.BeginEdit();
                        args.Item[Sitecore.Connector.CMP.Constants.EntityIdentifierFieldId] = args.EntityIdentifier;
                        flag = TryMapItemRelation(args);
                    }
                    catch
                    {
                        flag = false;
                        throw;
                    }
                    finally
                    {
                        if (flag)
                        {
                            args.Item.Editing.EndEdit();
                        }
                        else
                        {
                            args.Item.Editing.CancelEdit();
                            args.Item.Editing.BeginEdit();
                            args.Item[Sitecore.Connector.CMP.Constants.EntityIdentifierFieldId] = args.EntityIdentifier;
                            args.Item.Editing.EndEdit();
                        }
                    }
                }
            }
        }

        internal virtual bool TryMapItemRelation(ImportEntityPipelineArgs args)
        {
            if (args.EntityMappingItem == null)
                args.EntityMappingItem = _cmpHelper.GetEntityMappingItem(args);
            Assert.IsNotNull(args.EntityMappingItem, "Could not find any Entity Mapping item for the Entity Type (Schema): " + args.ContentTypeIdentifier);
            var flag1 = false;
            foreach (var relatedEntityMappingItem in args.EntityMappingItem.Children.Where(i => i.TemplateID == Sitecore.Connector.CMP.Constants.RelatedEntityMappingTemplateId))
            {
                var fieldName1 = relatedEntityMappingItem[Sitecore.Connector.CMP.Constants.FieldMappingSitecoreFieldNameFieldId];
                var cmpRelationName = relatedEntityMappingItem[Sitecore.Connector.CMP.Constants.RelationFieldMappingCmpRelationFieldNameFieldId];
                var relationshipTypeValue = relatedEntityMappingItem[Constants.Fields.RelationshipTypeField];
                var relationshipType = string.IsNullOrEmpty(relationshipTypeValue)
                    ? ID.Null
                    : new ID(relationshipTypeValue);
                if (!string.IsNullOrEmpty(fieldName1))
                {
                    if (!string.IsNullOrEmpty(cmpRelationName))
                    {
                        try
                        {
                            var database = _factory.GetDatabase(_settings.DatabaseName);
                            Assert.IsNotNull(database, "Could not get the master database.");
                            var relatedEntities = _cmpHelper.GetRelationEntities(args, cmpRelationName, relationshipType);
                            var cmpRootConfigItem = database.GetItem(Sitecore.Connector.CMP.Constants.ConfigItemId, args.Language);
                            Assert.IsNotNull(cmpRootConfigItem, "Could not get the CMP Config item.");
                            var source = new List<string>();
                            foreach (var relatedEntityModel in relatedEntities)
                            {
                                if (string.Equals(relatedEntityModel.EntityDefinition, "M.Asset", StringComparison.Ordinal) || string.Equals(relatedEntityModel.EntityDefinition, "M.AssetMedia", StringComparison.Ordinal))
                                {
                                    var originalPublicLinkEntity = (IEntity)null;
                                    var thumbnailPublicLinkEntity = (IEntity)null;
                                    if (relatedEntityModel != null)
                                    {
                                        originalPublicLinkEntity = _cmpHelper.GetAssetPublicLinkEntityAsync(relatedEntityModel.EntityId, "downloadoriginal").Result;
                                        thumbnailPublicLinkEntity = _cmpHelper.GetAssetPublicLinkEntityAsync(relatedEntityModel.EntityId, "thumbnail").Result;
                                    }
                                    if (originalPublicLinkEntity != null)
                                    {
                                        source.Add(_cmpHelper.GetPublicLinkHtmlAsync(relatedEntityModel.Entity, originalPublicLinkEntity, thumbnailPublicLinkEntity).Result);
                                    }
                                    break;
                                }
                                foreach (var relatedEntityMappingConfigItem in cmpRootConfigItem.Children.Where(x => x.TemplateID == Sitecore.Connector.CMP.Constants.EntityMappingTemplateId))
                                {
                                    if (!string.Equals(relatedEntityModel.EntityDefinition,
                                            relatedEntityMappingConfigItem[
                                                Sitecore.Connector.CMP.Constants.EntityMappingEntityTypeSchemaFieldId],
                                            StringComparison.Ordinal))
                                    {
                                        continue;
                                    }

                                    Assert.IsTrue(ID.TryParse(relatedEntityMappingConfigItem[Sitecore.Connector.CMP.Constants.EntityMappingBucketFieldId], out var result), "The entity mapping bucket Id is empty or invalid. Check this field value in the configuration item.");
                                    var itemBucket = database.GetItem(result, args.Language);
                                    Assert.IsNotNull(itemBucket, $"Could not get the Relation Bucket item id {result}.");


                                    var relatedItemId = _itemIdService.GenerateId(relatedEntityModel.Entity);
                                    if (ID.IsNullOrEmpty(relatedItemId))
                                    {
                                        Log.Error($"EnsureRelation: {relatedEntityModel.EntityIdentifier} failed to generate an Item ID", this);
                                        continue;
                                    }

                                    var relatedEntityItem = database.GetItem(relatedItemId, args.Language);
                                    if (relatedEntityItem == null)
                                    {
                                        continue;
                                    }
                                        
                                    source.Add(relatedItemId?.ToString());

                                    foreach (var relatedEntityFieldMapping in relatedEntityMappingConfigItem.Children.Where(x => x.TemplateID == Sitecore.Connector.CMP.Constants.RelatedEntityMappingTemplateId
                                                 && string.Equals(x[Sitecore.Connector.CMP.Constants.RelationFieldMappingCmpRelationFieldNameFieldId], cmpRelationName, StringComparison.Ordinal)))
                                    {
                                        var relatedEntityRelationshipTypeValue =
                                            relatedEntityFieldMapping[Constants.Fields.RelationshipTypeField];
                                        var relatedEntityRelationshipType = string.IsNullOrEmpty(relatedEntityRelationshipTypeValue)
                                            ? ID.Null
                                            : new ID(relatedEntityRelationshipTypeValue);
                                        if ((ID.IsNullOrEmpty(relatedEntityRelationshipType) || relatedEntityRelationshipType == Constants.RelationshipType.Default) 
                                            && (relationshipType == Constants.RelationshipType.Parent || relationshipType == Constants.RelationshipType.Child) )
                                        {
                                            //skip, both relationships types must be configured if at least one of them is
                                            Logger.Error(BaseHelper.GetLogMessageText(_settings.LogMessageTitle,
                                                    $"An error occured during mapping related entity of CMP Relation '{cmpRelationName}' to '{args.Item.Name}' item. Field mapping ID: '{relatedEntityMappingItem.ID}'. Both fields must have their relationship type set if at least one of them is set to parent or child"),
                                                this);
                                            continue;
                                        }

                                        if (relationshipType == relatedEntityRelationshipType)
                                        {
                                            //skip - its the same field
                                            continue;
                                        }

                                        var fieldTarget = relatedEntityFieldMapping[Sitecore.Connector.CMP.Constants.FieldMappingSitecoreFieldNameFieldId];
                                        var str = relatedEntityItem[fieldTarget];
                                        var alreadyRelated = false;
                                        var strArray = Array.Empty<string>();
                                        if (!string.IsNullOrEmpty(str))
                                        {
                                            strArray = str.Split("|".ToCharArray());
                                            alreadyRelated = strArray.Any(x => string.Equals(x, args.Item.ID.ToString(), StringComparison.Ordinal));
                                        }
                                        if (!alreadyRelated)
                                        {
                                            var list = strArray.ToList();
                                            list.Add(args.Item.ID.ToString());
                                            relatedEntityItem.Editing.BeginEdit();
                                            
                                            relatedEntityItem[fieldTarget] = strArray.Length != 0 ? string.Join("|", list) : args.Item.ID.ToString();
                                                
                                            relatedEntityItem.Editing.EndEdit();
                                        }
                                    }
                                }
                            }
                            args.Item[fieldName1] = source.Count != 0 ? string.Join("|", source.Distinct().ToList()) : string.Empty;
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(BaseHelper.GetLogMessageText(_settings.LogMessageTitle,
                                $"An error occured during mapping related entity of CMP Relation '{cmpRelationName}' to '{args.Item.Name}' item. Field mapping ID: '{relatedEntityMappingItem.ID}'."), ex, this);
                            flag1 = true;
                            args.Exception = ex;
                            continue;
                        }
                    }
                }
                Logger.Error(BaseHelper.GetLogMessageText(_settings.LogMessageTitle,
                    $"Configuration of the field mapping '{relatedEntityMappingItem.ID}' is incorrect. Required fields are not specified."), this);
                flag1 = true;
            }
            return !flag1;
        }
    }
}