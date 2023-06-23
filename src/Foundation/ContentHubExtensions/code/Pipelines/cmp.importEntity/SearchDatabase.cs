using Foundation.ContentHubExtensions.Services;
using Sitecore.Abstractions;
using Sitecore.Connector.CMP;
using Sitecore.Connector.CMP.Helpers;
using Sitecore.Connector.CMP.Pipelines.ImportEntity;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace Foundation.ContentHubExtensions.Pipelines.cmp.importEntity
{
    public class SearchDatabase : ImportEntityProcessor
    {
        private static CmpSettings _settings;
        private readonly BaseFactory _factory;
        private readonly CmpHelper _cmpHelper;
        private readonly IItemIdService _itemIdService;

        public SearchDatabase(
            BaseLog logger, 
            CmpSettings settings, 
            BaseFactory factory,
            CmpHelper cmpHelper,
            IItemIdService itemIdService) : base(logger, settings)
        {
            _factory = factory;
            _settings = settings;
            _cmpHelper = cmpHelper;
            _itemIdService = itemIdService;
        }

        public override void Process(ImportEntityPipelineArgs args, BaseLog logger)
        {
            if (args.Item != null)
                return;
            if (args.EntityMappingItem == null)
                args.EntityMappingItem = this._cmpHelper.GetEntityMappingItem(args);
            Assert.IsNotNull((object)args.EntityMappingItem, "Could not find any Entity Mapping item for the Entity Type (Schema): " + args.ContentTypeIdentifier);
            Database database = this._factory.GetDatabase(_settings.DatabaseName);
            Assert.IsNotNull((object)database, "Could not get the master database.");
            Item cmpItemBucket = this._cmpHelper.GetCmpItemBucket(args, database);
            Assert.IsNotNull((object)cmpItemBucket, "Could not find the item bucket. Check this field value in the configuration item.");
            Assert.IsNotNull((object)args.EntityIdentifier, "Could not get entity identifier.");

            var itemId = _itemIdService.GenerateId(args.Entity);
            if (ID.IsNullOrEmpty(itemId))
            {
                Log.Error($"SearchDatabase: {args.EntityIdentifier} failed to generate an Item ID", this);
                return;
            }
            
            args.Item = database.GetItem(itemId, args.Language);
        }
    }
}