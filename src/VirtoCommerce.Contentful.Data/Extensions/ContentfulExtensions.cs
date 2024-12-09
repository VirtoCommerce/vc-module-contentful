using VirtoCommerce.Pages.Core.Events;

namespace VirtoCommerce.Contentful.Data.Extensions;

public static class ContentfulExtensions
{
    public static PageOperation ToPageOperation(this string value)
    {
        return value switch
        {
            "ContentManagement.Entry.archive" => PageOperation.Archive,
            "ContentManagement.Entry.delete" => PageOperation.Delete,
            "ContentManagement.Entry.publish" => PageOperation.Publish,
            "ContentManagement.Entry.unpublish" => PageOperation.Unpublish,
            _ => PageOperation.Unknown
        };
    }
}
