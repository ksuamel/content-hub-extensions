using Sitecore.Data;
using Stylelabs.M.Sdk.Contracts.Base;

namespace Foundation.ContentHubExtensions.Services
{
    public interface IItemIdService
    {
        ID GenerateId(IEntity entity);
    }
}