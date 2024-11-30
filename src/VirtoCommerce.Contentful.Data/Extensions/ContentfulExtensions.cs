using VirtoCommerce.Pages.Core.Events;

namespace VirtoCommerce.Contentful.Data.Extensions;

public static class ContentfulExtensions
{
    public static PageOperation ToPageOperation(this string value)
    {
        return value switch
        {
            "ContentManagement.Entry.archive" => PageOperation.Archive,
            // "ContentManagement.Entry.save" => PageOperation.Unknown,
            // "ContentManagement.Entry.create" => PageOperation.Unknown,
            // "ContentManagement.Entry.auto_save" => PageOperation.Unknown,
            // "ContentManagement.Entry.unarchive" => PageOperation.Unpublish,
            "ContentManagement.Entry.delete" => PageOperation.Delete,
            "ContentManagement.Entry.publish" => PageOperation.Publish,
            "ContentManagement.Entry.unpublish" => PageOperation.Unpublish,
            _ => PageOperation.Unknown
        };
    }
}
