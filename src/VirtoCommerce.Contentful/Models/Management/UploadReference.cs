using Newtonsoft.Json;

namespace VirtoCommerce.Contentful.Models.Management
{
    /// <summary>
    /// Represents information about an uploaded binary file that can be used to create an asset.
    /// </summary>
    public class UploadReference : IContentfulResource
    {
        /// <summary>
        /// Common system managed metadata properties.
        /// </summary>
        [JsonProperty("sys")]
        public SystemProperties SystemProperties { get; set; }
    }
}
