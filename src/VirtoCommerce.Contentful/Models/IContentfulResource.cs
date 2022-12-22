namespace VirtoCommerce.Contentful.Models
{
    /// <summary>
    /// Represents a single Contentful resource.
    /// </summary>
    public interface IContentfulResource
    {
        /// <summary>
        /// Common system managed metadata properties.
        /// </summary>
        SystemProperties SystemProperties { get; set; }
    }
}
