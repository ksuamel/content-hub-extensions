using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Foundation.ContentHubExtensions.Utils
{
    public static class ItemUtil
    {
        public static ID IdProvidersFolderId = new ID("{BB96F664-8E77-4980-89FE-BF5D5D9922DC}");
        public static ID ConfigItemId = new ID("{F8843BF7-A31A-4F71-8900-ABD56B8FE34C}");

        public static Database SourceDatabase = Factory.GetDatabase("master");

        public static Item GetIdProviderItem()
        {
            return SourceDatabase.GetItem(IdProvidersFolderId);
        }

        public static Item GetConfigItem()
        {
            return SourceDatabase.GetItem(ConfigItemId);
        }
    }
}