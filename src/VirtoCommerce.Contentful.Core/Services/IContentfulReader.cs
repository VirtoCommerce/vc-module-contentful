using VirtoCommerce.Contentful.Core.Models;

namespace VirtoCommerce.Contentful.Core.Services;

public interface IContentfulReader
{
    Task<ContentfulEntry> ReadEntry(Stream stream);
}
