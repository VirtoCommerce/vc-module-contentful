namespace VirtoCommerce.Contentful.Core.Services;

public interface IContentfulRenderer
{
    Task<string> RenderContent(string value);
}
