using Sitecore.Data;

namespace Foundation.ContentHubExtensions
{
    public static class Constants
    {
        public static class RelationshipType
        {
            public static ID Default = new ID("{76E24362-AB03-40A4-B693-0CE3666CB7A7}");
            public static ID Parent = new ID("{F5341070-7CE9-4671-B790-8A30CC549C83}");
            public static ID Child = new ID("{E27B936A-CE76-493C-AE9E-DBDA1190541D}");
        }

        public static class Fields
        {
            public static ID RelationshipTypeField = new ID("{83F5FF5D-EAB4-4EFD-88EC-6ACA36A545DA}");
        }
    }
}