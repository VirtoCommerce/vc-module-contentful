using System.Reflection;
using Contentful.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VirtoCommerce.Contentful.Core.Models;
using VirtoCommerce.Contentful.Core.Services;

namespace VirtoCommerce.Contentful.Data.Services;

public class ContentfulReader : IContentfulReader
{
    public async Task<ContentfulEntry> ReadEntry(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        var source = JsonConvert.DeserializeObject<JObject>(json);

        var entry = GetEntry(source);

        entry.EntryType = GetEntryType(entry.SystemProperties.ContentType.SystemProperties.Id);

        return entry;
    }

    private static ContentfulEntry GetEntry(JObject source)
    {
        ContentfulEntry result;

        if (typeof(IContentfulResource).GetTypeInfo().IsAssignableFrom(typeof(ContentfulEntry).GetTypeInfo()))
        {
            result = source.ToObject<ContentfulEntry>();
        }
        else
        {
            var json = source;

            //move the sys object beneath the fields to make serialization more logical for the end user.
            var sys = json.SelectToken("$.sys");
            var fields = json.SelectToken("$.fields");
            fields["sys"] = sys;
            result = fields.ToObject<ContentfulEntry>();
        }
        return result;
    }

    private static EntryType GetEntryType(string entryType)
    {
        if (entryType.StartsWith("page")) // we only support pages for now
        {
            return EntryType.Page;
        }
        if (entryType.StartsWith("product"))
        {
            return EntryType.Product;
        }

        return EntryType.Unknown;
    }
}
