namespace VirtoCommerce.Contentful.Core;

public static class ContentfulConstants
{
    public static class Security
    {
        public static class Permissions
        {
            public const string Read = "content:read",
                Create = "content:create",
                Access = "content:access",
                Update = "content:update",
                Delete = "content:delete";

            public static readonly string[] AllPermissions = [Read, Create, Access, Update, Delete];
        }
    }
}
