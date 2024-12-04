namespace VirtoCommerce.Contentful.Core;

public static class ContentfulConstants
{
    public static class Security
    {
        public static class Permissions
        {
            public const string Read = "contentful:read";
            public const string Create = "content:create";
            public const string Access = "content:access";
            public const string Update = "content:update";
            public const string Delete = "content:delete";

            public static readonly string[] AllPermissions = [Read, Create, Access, Update, Delete];
        }
    }
}
