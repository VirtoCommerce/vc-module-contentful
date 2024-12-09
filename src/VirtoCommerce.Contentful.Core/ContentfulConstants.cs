namespace VirtoCommerce.Contentful.Core;

public static class ContentfulConstants
{
    public static class Security
    {
        public static class Permissions
        {
            public const string Read = "contentful:read";
            public const string Create = "contentful:create";
            public const string Access = "contentful:access";
            public const string Update = "contentful:update";
            public const string Delete = "contentful:delete";

            public static readonly string[] AllPermissions = [Read, Create, Access, Update, Delete];
        }
    }
}
