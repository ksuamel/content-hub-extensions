using System.Collections.Generic;
using Foundation.ContentHubExtensions.ItemIdProviders;
using Foundation.ContentHubExtensions.ItemIdProviders.Common;
using MediatR;
using Sitecore.Abstractions;
using Sitecore.Connector.CMP;
using Sitecore.Connector.CMP.Pipelines.ImportEntity;

namespace Foundation.ContentHubExtensions.Pipelines.cmp.importEntity
{
    public class PostSaveEntity : ImportEntityProcessor
    {
        private readonly IMediator _mediator;
        private readonly IEnumerable<IItemIdProvider> _itemIdProviders;

        public PostSaveEntity(
            BaseLog logger, 
            CmpSettings settings, 
            IMediator mediator, 
            IEnumerable<IItemIdProvider> itemIdProviders) : base(logger, settings)
        {
            _mediator = mediator;
            _itemIdProviders = itemIdProviders;
        }

        public override void Process(ImportEntityPipelineArgs args, BaseLog logger)
        {
            var idProvider = DefaultItemIdProvider.Default;
            foreach (var itemIdProvider in _itemIdProviders)
            {
                if (!itemIdProvider.CanProcess(args.Entity))
                {
                    continue;
                }

                idProvider = itemIdProvider;
            }
            
            //_mediator.Publish(new EntitySaved
            //{
            //    Type = args.Entity.DefinitionName,
            //    EntityId = idProvider.GetEntityId(args.Entity),
            //    ItemId = args.Item.ID.Guid
            //}).ConfigureAwait(false);
        }
    }
}