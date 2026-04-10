using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.Contentful.Core;

public static class ContentfulConstants
{
    public const string PageContentTypePrefix = "page";

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

    public static class Settings
    {
        public static class General
        {
            public static SettingDescriptor SpaceId { get; } = new()
            {
                Name = "Contentful.SpaceId",
                GroupName = "CMS|Contentful",
                ValueType = SettingValueType.ShortText,
                DefaultValue = string.Empty,
            };

            public static SettingDescriptor DeliveryApiKey { get; } = new()
            {
                Name = "Contentful.DeliveryApiKey",
                GroupName = "CMS|Contentful",
                ValueType = SettingValueType.SecureString,
                DefaultValue = string.Empty,
            };

            public static SettingDescriptor ContentTypeId { get; } = new()
            {
                Name = "Contentful.ContentTypeId",
                GroupName = "CMS|Contentful",
                ValueType = SettingValueType.ShortText,
                DefaultValue = PageContentTypePrefix,
            };
        }

        public static IEnumerable<SettingDescriptor> AllSettings
        {
            get
            {
                yield return General.SpaceId;
                yield return General.DeliveryApiKey;
                yield return General.ContentTypeId;
            }
        }

        public static IEnumerable<SettingDescriptor> StoreLevelSettings
        {
            get
            {
                yield return General.SpaceId;
                yield return General.DeliveryApiKey;
                yield return General.ContentTypeId;
            }
        }
    }
}
