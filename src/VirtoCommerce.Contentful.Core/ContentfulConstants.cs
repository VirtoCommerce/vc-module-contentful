using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.Contentful.Core;

public static class ContentfulConstants
{
    public const string PageContentTypePrefix = "page";
    private const string SettingsGroupName = "CMS|Contentful";

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
                GroupName = SettingsGroupName,
                ValueType = SettingValueType.ShortText,
                DefaultValue = string.Empty,
            };

            public static SettingDescriptor DeliveryApiKey { get; } = new()
            {
                Name = "Contentful.DeliveryApiKey",
                GroupName = SettingsGroupName,
                ValueType = SettingValueType.SecureString,
                DefaultValue = string.Empty,
            };

            public static SettingDescriptor ContentTypeId { get; } = new()
            {
                Name = "Contentful.ContentTypeId",
                GroupName = SettingsGroupName,
                ValueType = SettingValueType.ShortText,
                DefaultValue = PageContentTypePrefix,
            };

            public static SettingDescriptor PreviewApiKey { get; } = new()
            {
                Name = "Contentful.PreviewApiKey",
                GroupName = SettingsGroupName,
                ValueType = SettingValueType.SecureString,
                DefaultValue = string.Empty,
            };
        }

        public static IEnumerable<SettingDescriptor> AllSettings
        {
            get
            {
                yield return General.SpaceId;
                yield return General.DeliveryApiKey;
                yield return General.ContentTypeId;
                yield return General.PreviewApiKey;
            }
        }

        public static IEnumerable<SettingDescriptor> StoreLevelSettings
        {
            get
            {
                yield return General.SpaceId;
                yield return General.DeliveryApiKey;
                yield return General.ContentTypeId;
                yield return General.PreviewApiKey;
            }
        }
    }
}
