using Foundation.ContentHubExtensions.Services;
using Sitecore.Abstractions;
using Sitecore.Connector.CMP;
using Sitecore.Connector.CMP.Helpers;
using Sitecore.Connector.CMP.Pipelines.ImportEntity;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;

namespace Foundation.ContentHubExtensions.Pipelines.cmp.importEntity
{
    public class EnsureItem : ImportEntityProcessor
    {
        private static CmpSettings _settings;
        private readonly BaseFactory _factory;
        private readonly CmpHelper _cmpHelper;
        private readonly IItemIdService _itemIdService;

        public EnsureItem(
          BaseFactory factory,
          BaseLog logger,
          CmpSettings cmpSettings,
          CmpHelper cmpHelper, 
          IItemIdService itemIdService)
          : base(logger, cmpSettings)
        {
            this._factory = factory;
            EnsureItem._settings = cmpSettings;
            this._cmpHelper = cmpHelper;
            _itemIdService = itemIdService;
        }

        public override void Process(ImportEntityPipelineArgs args, BaseLog logger)
        {
            if (args.Item != null)
            {
                if (args.EntityDefinition == null || string.IsNullOrEmpty(args.EntityDefinition.Name))
                    return;
                //Sitecore.Connector.CMP.ConsumptionTracking.Telemetry.TrackCMPUpdateEntity(args.EntityDefinition.Name);
            }
            else
            {
                if (args.EntityMappingItem == null)
                    args.EntityMappingItem = this._cmpHelper.GetEntityMappingItem(args);
                Assert.IsNotNull((object)args.EntityMappingItem, "Could not find any Entity Mapping item for the Entity Type (Schema): " + args.ContentTypeIdentifier);
                using (new SecurityDisabler())
                {
                    Database database = this._factory.GetDatabase(EnsureItem._settings.DatabaseName);
                    Assert.IsNotNull((object)database, "Could not get the master database.");
                    Item cmpItemBucket = this._cmpHelper.GetCmpItemBucket(args, database);
                    Assert.IsNotNull((object)cmpItemBucket, "Could not find the item bucket. Check this field value in the configuration item.");
                    string propertyValue = args.Entity.GetPropertyValue<string>(args.EntityMappingItem[Sitecore.Connector.CMP.Constants.EntityMappingItemNamePropertyField]);
                    Assert.IsNotNullOrEmpty(propertyValue, "Could not get the property value from Content Hub as Sitecore item name. Check this field isn't valid and the value should not be null in Content Hub.");
                    string name = ItemUtil.ProposeValidItemName(propertyValue);
                    Assert.IsNotNullOrEmpty(name, "Could not proposed the valid item name as Sitecore Item Name.");
                    TemplateItem template = (TemplateItem)database.GetItem(new ID(args.EntityMappingItem[Sitecore.Connector.CMP.Constants.EntityMappingTemplateFieldId]), args.Language);
                    Assert.IsNotNull((object)template, "Could not get template item. Check this field value in the configuration item.");

                    if (args.EntityDefinition == null || string.IsNullOrEmpty(args.EntityDefinition.Name))
                    {
                        //Sitecore.Connector.CMP.ConsumptionTracking.Telemetry.TrackCMPInsertEntity(args.EntityDefinition.Name);
                        Log.Error($"EnsureItem: {args.EntityIdentifier} did not have a valid EntityDefinition", this);
                        return;
                    }

                    var itemId = _itemIdService.GenerateId(args.Entity);
                    if (ID.IsNullOrEmpty(itemId))
                    {
                        Log.Error($"EnsureItem: {args.EntityIdentifier} failed to generate an Item ID", this);
                        return;
                    }

                    args.Item = cmpItemBucket.Add(name, template, itemId);
                }
            }
        }
    }
}