using Newtonsoft.Json;

namespace VirtoCommerce.Contentful.Models
{
    /// <summary>
    /// Represents an asset returned by a <see cref="SyncResult"/>.
    /// </summary>
    public class SyncedAsset : IContentfulResource
    {
        /// <summary>
        /// Common system managed metadata properties.
        /// </summary>
        [JsonProperty("sys")]
        public SystemProperties SystemProperties { get; set; }

        /// <summary>
        /// The fields of the synced asset serialized into a dynamic object.
        /// </summary>
        public dynamic Fields { get; set; }
    }
}
