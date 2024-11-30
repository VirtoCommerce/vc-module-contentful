using Contentful.Core.Configuration;
using Contentful.Core.Models;
using Newtonsoft.Json;
using VirtoCommerce.Contentful.Core.Services;

namespace VirtoCommerce.Contentful.Data.Services;

public class ContentfulRenderer : IContentfulRenderer
{
    public async Task<string> RenderContent(string value)
    {
        try
        {
            var document = JsonConvert.DeserializeObject<Document>(value,
                new JsonSerializerSettings
                {
                    Converters =
                    {
                        new AssetJsonConverter(),
                        new ContentJsonConverter()
                    }
                });
            var renderer = new HtmlRenderer();
            var result = await renderer.ToHtml(document);
            return result;
        }
        catch
        {
            return value;
        }
    }
}
