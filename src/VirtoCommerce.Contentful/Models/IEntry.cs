namespace VirtoCommerce.Contentful.Models
{
    /// <summary>
    /// Represents a Contentful entry resource.
    /// </summary>
    /// <typeparam name="T">The type the fields of the entry should be serialized into.</typeparam>
    public interface IEntry<T> : IContentfulResource
    {
        /// <summary>
        /// The fields of the entry deserialized to the type T.
        /// </summary>
        T Fields { get; set; }
    }
}
