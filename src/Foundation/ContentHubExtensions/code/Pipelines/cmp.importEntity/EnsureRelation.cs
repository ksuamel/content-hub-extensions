using Foundation.ContentHubExtensions.Helpers;
using Foundation.ContentHubExtensions.Services;
using Sitecore.Abstractions;
using Sitecore.Connector.CMP;
using Sitecore.Connector.CMP.Helpers;
using Sitecore.Connector.CMP.Models;
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
        private readonly SitecoreHelper _sitecoreHelper;
        private readonly IItemIdService _itemIdService;

        public EnsureRelation(
          BaseFactory factory,
          CustomCmpHelper cmpHelper,
          SitecoreHelper sitecoreHelper,
          BaseLog logger,
          CmpSettings settings, 
          IItemIdService itemIdService)
          : base(logger, settings)
        {
            this._factory = factory;
            EnsureRelation._settings = settings;
            this._cmpHelper = cmpHelper;
            this._sitecoreHelper = sitecoreHelper;
            _itemIdService = itemIdService;
        }

        public override void Process(ImportEntityPipelineArgs args, BaseLog logger)
        {
            Assert.IsNotNull((object)args.Item, "The item is null.");
            Assert.IsNotNull((object)args.Language, "The language is null.");
            using (new SecurityDisabler())
            {
                using (new LanguageSwitcher(args.Language))
                {
                    bool flag = false;
                    try
                    {
                        args.Item.Editing.BeginEdit();
                        args.Item[Sitecore.Connector.CMP.Constants.EntityIdentifierFieldId] = args.EntityIdentifier;
                        flag = this.TryMapItemRelation(args);
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
                args.EntityMappingItem = this._cmpHelper.GetEntityMappingItem(args);
            Assert.IsNotNull((object)args.EntityMappingItem, "Could not find any Entity Mapping item for the Entity Type (Schema): " + args.ContentTypeIdentifier);
            bool flag1 = false;
            foreach (Item relatedEntityMappingItem in args.EntityMappingItem.Children.Where<Item>((Func<Item, bool>)(i => i.TemplateID == Sitecore.Connector.CMP.Constants.RelatedEntityMappingTemplateId)))
            {
                string fieldName1 = relatedEntityMappingItem[Sitecore.Connector.CMP.Constants.FieldMappingSitecoreFieldNameFieldId];
                string cmpRelationName = relatedEntityMappingItem[Sitecore.Connector.CMP.Constants.RelationFieldMappingCmpRelationFieldNameFieldId];
                if (!string.IsNullOrEmpty(fieldName1))
                {
                    if (!string.IsNullOrEmpty(cmpRelationName))
                    {
                        try
                        {
                            Database database = this._factory.GetDatabase(EnsureRelation._settings.DatabaseName);
                            Assert.IsNotNull((object)database, "Could not get the master database.");
                            List<CmpEntityModel> relationEntities = this._cmpHelper.GetRelationEntities(args, cmpRelationName);
                            Item cmpRootConfigItem = database.GetItem(Sitecore.Connector.CMP.Constants.ConfigItemId, args.Language);
                            Assert.IsNotNull((object)cmpRootConfigItem, "Could not get the CMP Config item.");
                            List<string> source = new List<string>();
                            foreach (CmpEntityModel relatedEntityModel in relationEntities)
                            {
                                if (string.Equals(relatedEntityModel.EntityDefinition, "M.Asset", StringComparison.Ordinal) || string.Equals(relatedEntityModel.EntityDefinition, "M.AssetMedia", StringComparison.Ordinal))
                                {
                                    IEntity originalPublicLinkEntity = (IEntity)null;
                                    IEntity thumbnailPublicLinkEntity = (IEntity)null;
                                    if (relatedEntityModel != null)
                                    {
                                        originalPublicLinkEntity = this._cmpHelper.GetAssetPublicLinkEntityAsync(relatedEntityModel.EntityId, "downloadoriginal").Result;
                                        thumbnailPublicLinkEntity = this._cmpHelper.GetAssetPublicLinkEntityAsync(relatedEntityModel.EntityId, "thumbnail").Result;
                                    }
                                    if (originalPublicLinkEntity != null)
                                    {
                                        source.Add(this._cmpHelper.GetPublicLinkHtmlAsync(relatedEntityModel.Entity, originalPublicLinkEntity, thumbnailPublicLinkEntity).Result);
                                        break;
                                    }
                                    break;
                                }
                                foreach (Item entityMappingConfigItem in cmpRootConfigItem.Children.Where<Item>((Func<Item, bool>)(x => x.TemplateID == (ID)Sitecore.Connector.CMP.Constants.EntityMappingTemplateId)))
                                {
                                    if (string.Equals(relatedEntityModel.EntityDefinition, entityMappingConfigItem[Sitecore.Connector.CMP.Constants.EntityMappingEntityTypeSchemaFieldId], StringComparison.Ordinal))
                                    {
                                        ID result;
                                        Assert.IsTrue(ID.TryParse(entityMappingConfigItem[Sitecore.Connector.CMP.Constants.EntityMappingBucketFieldId], out result), "The entity mapping bucket Id is empty or invalid. Check this field value in the configuration item.");
                                        Item itemBucket = database.GetItem(result, args.Language);
                                        Assert.IsNotNull((object)itemBucket, string.Format("Could not get the Relation Bucket item id {0}.", (object)result));


                                        var relatedItemId = _itemIdService.GenerateId(relatedEntityModel.Entity);
                                        if (ID.IsNullOrEmpty(relatedItemId))
                                        {
                                            Log.Error($"EnsureRelation: {relatedEntityModel.EntityIdentifier} failed to generate an Item ID", this);
                                            continue;
                                        }

                                        Item relatedEntityItem = database.GetItem(relatedItemId, args.Language);
                                        if (relatedEntityItem == null)
                                        {
                                            continue;
                                        }
                                        
                                        source.Add(relatedItemId?.ToString());

                                        foreach (BaseItem baseItem in entityMappingConfigItem.Children.Where<Item>((Func<Item, bool>)(x => x.TemplateID == Sitecore.Connector.CMP.Constants.RelatedEntityMappingTemplateId && string.Equals(x[Sitecore.Connector.CMP.Constants.RelationFieldMappingCmpRelationFieldNameFieldId], cmpRelationName, StringComparison.Ordinal))))
                                        {
                                            string fieldName2 = baseItem[Sitecore.Connector.CMP.Constants.FieldMappingSitecoreFieldNameFieldId];
                                            string str = relatedEntityItem[fieldName2];
                                            bool flag2 = false;
                                            string[] strArray = Array.Empty<string>();
                                            if (!string.IsNullOrEmpty(str))
                                            {
                                                strArray = str.Split("|".ToCharArray());
                                                flag2 = ((IEnumerable<string>)strArray).Any<string>((Func<string, bool>)(x => string.Equals(x, args.Item.ID.ToString(), StringComparison.Ordinal)));
                                            }
                                            if (!flag2)
                                            {
                                                List<string> list = ((IEnumerable<string>)strArray).ToList<string>();
                                                list.Add(args.Item.ID.ToString());
                                                relatedEntityItem.Editing.BeginEdit();

                                                var relatedEntityReverseRelationshipFieldName =
                                                    GetRelatedEntityReverseRelationshipFieldName(fieldName2,
                                                        cmpRelationName, entityMappingConfigItem);
                                                relatedEntityItem[relatedEntityReverseRelationshipFieldName] = strArray.Length != 0 ? string.Join("|", (IEnumerable<string>)list) : args.Item.ID.ToString();
                                                
                                                relatedEntityItem.Editing.EndEdit();
                                            }
                                        }
                                    }
                                }
                            }
                            args.Item[fieldName1] = source.Count != 0 ? string.Join("|", (IEnumerable<string>)source.Distinct<string>().ToList<string>()) : string.Empty;
                            continue;
                        }
                        catch (Exception ex)
                        {
                            this.Logger.Error(BaseHelper.GetLogMessageText(EnsureRelation._settings.LogMessageTitle, string.Format("An error occured during mapping related entity of CMP Relation '{0}' to '{1}' item. Field mapping ID: '{2}'.", (object)cmpRelationName, (object)args.Item.Name, (object)relatedEntityMappingItem.ID)), ex, (object)this);
                            flag1 = true;
                            args.Exception = ex;
                            continue;
                        }
                    }
                }
                this.Logger.Error(BaseHelper.GetLogMessageText(EnsureRelation._settings.LogMessageTitle, string.Format("Configuration of the field mapping '{0}' is incorrect. Required fields are not specified.", (object)relatedEntityMappingItem.ID)), (object)this);
                flag1 = true;
            }
            return !flag1;
        }

        private string GetRelatedEntityReverseRelationshipFieldName(string relatedEntityFieldName, string cmpRelationName, Item entityMappingConfigItem)
        {
            string counterPartCmpRelationName = relatedEntityFieldName;

            foreach (BaseItem counterPartBaseItem in
                     entityMappingConfigItem.Children.Where<Item>(
                         (Func<Item, bool>)(x =>
                             x.TemplateID == Sitecore.Connector.CMP.Constants
                                 .RelatedEntityMappingTemplateId &&
                             string.Equals(
                                 x[
                                     Sitecore.Connector.CMP.Constants
                                         .RelationFieldMappingCmpRelationFieldNameFieldId],
                                 counterPartCmpRelationName, StringComparison.Ordinal))))
            {
                return counterPartBaseItem[Sitecore.Connector.CMP.Constants.FieldMappingSitecoreFieldNameFieldId];
            }

            return relatedEntityFieldName;
        }
    }
}