using System.Collections.Generic;
using Sitecore.Connector.CMP;
using Sitecore.Connector.CMP.Conversion;
using Sitecore.Connector.CMP.Helpers;
using Sitecore.Connector.CMP.Models;
using Sitecore.Connector.CMP.Pipelines.ImportEntity;
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

        public override List<CmpEntityModel> GetRelationEntities(ImportEntityPipelineArgs args, string cmpRelationName)
        {
            if (!cmpRelationName.EndsWith(Constants.ChildSuffix) && !cmpRelationName.EndsWith(Constants.ParentSuffix))
            {
                return base.GetRelationEntities(args, cmpRelationName);
            }

            var isChild = cmpRelationName.EndsWith(Constants.ChildSuffix);
            var cleanRelationName = cmpRelationName.Replace(Constants.ChildSuffix, "").Replace(Constants.ParentSuffix, "");
            var longList = (IList<long>)null;
            var relation = args.Entity.GetRelation(cleanRelationName, new RelationRole?( isChild  ? RelationRole.Child : RelationRole.Parent));
            if (relation != null)
                longList = relation.GetIds();
            var relationEntities = new List<CmpEntityModel>();

            if (longList != null)
            {
                foreach (long entityId in (IEnumerable<long>)longList)
                {
                    IEntity entity = this.GetEntity(entityId);
                    relationEntities.Add(new CmpEntityModel()
                    {
                        EntityId = entityId,
                        EntityDefinition = this.GetEntityDefinitionType(entity),
                        EntityIdentifier = entity.Identifier,
                        Entity = entity
                    });
                }
            }
            return relationEntities;
        }
    }
}