using System.Globalization;
using Contentful.Core.Models;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Contentful.Core.Models;

public class ContentfulEntry : Entry<Dictionary<string, Dictionary<string, object>>>
{
    public EntryType EntryType { get; set; }
    public string CultureName { get; set; }

    public virtual PageDocument ToPageDocument()
    {
        var result = AbstractTypeFactory<PageDocument>.TryCreateInstance();

        result.CreatedBy = SystemProperties.CreatedBy.SystemProperties.Id;
        result.CreatedDate = SystemProperties.CreatedAt ?? DateTime.Now;
        result.Id = SystemProperties.Id;
        result.OuterId = SystemProperties.Id;
        result.Permalink = GetField("permalink");
        if (Fields.TryGetValue("userGroups", out var userGroups))
        {
            result.UserGroups = (string[])userGroups[CultureName];
        }

        result.Title = GetField("title");
        result.Description = GetField("description");
        result.MimeType = "text/html";
        result.ModifiedBy = SystemProperties.UpdatedBy.SystemProperties.Id;
        result.ModifiedDate = SystemProperties.UpdatedAt;
        result.Source = "contentful";
        result.Visibility = Fields.TryGetValue("isAuthenticated", out var visibility)
            ? (bool)visibility[CultureName]
                ? PageDocumentVisibility.Private
                : PageDocumentVisibility.Public
            : PageDocumentVisibility.Private;
        result.StartDate = GetDateField("startDate", DateTime.MinValue);
        result.EndDate = GetDateField("endDate", DateTime.MaxValue);
        result.CultureName = CultureName;

        return result;
    }

    private string GetField(string fieldName)
    {
        return Fields.TryGetValue(fieldName, out var field)
            ? field[CultureName]?.ToString()
            : null;
    }

    private DateTime GetDateField(string fieldName, DateTime defaultValue)
    {
        var value = GetField(fieldName);
        // date format like "2024-11-15T01:00+02:00"
        return DateTime.TryParseExact(value, "yyyy-MM-ddTHH:mmzzz", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : defaultValue;
    }
}
