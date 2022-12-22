namespace VirtoCommerce.Contentful.Models
{
    /// <summary>
    /// Represents detailed information about a <see cref="File"/>.
    /// </summary>
    public class FileDetails
    {
        /// <summary>
        /// The size of the file in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// The image details of the file if applicable, will be null for non image types.
        /// </summary>
        public ImageDetails Image { get; set; }

    }
}
