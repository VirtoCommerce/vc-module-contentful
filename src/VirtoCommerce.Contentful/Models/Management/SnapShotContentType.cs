using Newtonsoft.Json;

namespace VirtoCommerce.Contentful.Models.Management
{
    /// <summary>
    /// Represents the snapshot of a content type at a given time in the past.
    /// </summary>
    public class SnapshotContentType : IContentfulResource
    {
        /// <summary>
        /// Common system managed metadata properties.
        /// </summary>
        [JsonProperty("sys")]
        public SystemProperties SystemProperties { get; set; }

        /// <summary>
        /// The snapshotted content type
        /// </summary>
        public ContentType Snapshot { get; set; }
    }
}
