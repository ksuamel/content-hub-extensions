using System.Collections.Generic;
using Sitecore.Connector.CMP;
using Sitecore.Connector.CMP.Conversion;
using Sitecore.Connector.CMP.Helpers;
using Sitecore.Connector.CMP.Models;
using Sitecore.Connector.CMP.Pipelines.ImportEntity;
using Sitecore.Data;
using Stylelabs.M.Framework.Essentials.LoadOptions;
using Stylelabs.M.Sdk.Contracts.Base;
using Stylelabs.M.Sdk.WebClient;

namespace Foundation.ContentHubExtensions.Helpers
{
    public class CustomCmpHelper : CmpHelper
    {
        public CustomCmpHelper(IWebMClient mClient, ICmpConverterMapper mapper, CmpSettings settings) : base(mClient, mapper, settings)
        {
        }

        public List<CmpEntityModel> GetRelationEntities(ImportEntityPipelineArgs args, string cmpRelationName, ID relationshipTypeId)
        {
            if (ID.IsNullOrEmpty(relationshipTypeId) || relationshipTypeId == Constants.RelationshipType.Default)
            {
                base.GetRelationEntities(args, cmpRelationName);
            }

            var isChild = relationshipTypeId == Constants.RelationshipType.Child;
            var longList = (IList<long>)null;
            var relation = args.Entity.GetRelation(cmpRelationName, isChild ? RelationRole.Child : RelationRole.Parent);
            if (relation != null)
                longList = relation.GetIds();
            var relationEntities = new List<CmpEntityModel>();

            if (longList == null)
            {
                return relationEntities;
            }

            foreach (var entityId in longList)
            {
                var entity = this.GetEntity(entityId);
                relationEntities.Add(new CmpEntityModel()
                {
                    EntityId = entityId,
                    EntityDefinition = this.GetEntityDefinitionType(entity),
                    EntityIdentifier = entity.Identifier,
                    Entity = entity
                });
            }
            return relationEntities;
        }
    }
}