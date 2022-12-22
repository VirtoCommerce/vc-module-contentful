using Newtonsoft.Json;
using System.Collections.Generic;
using VirtoCommerce.Contentful.Configuration;

namespace VirtoCommerce.Contentful.Models.Management
{
    /// <summary>
    /// Represents a collection of permissions for different parts of the Contentful APIs.
    /// </summary>
    public class ContentfulPermissions
    {
        /// <summary>
        /// Adds permissions to modify the content model.
        /// </summary>
        [JsonProperty("ContentModel")]
        [JsonConverter(typeof(StringAllToListJsonConverter))]
        public List<string> ContentModel { get; set; }

        /// <summary>
        /// Adds permissions to modify settings.
        /// </summary>
        [JsonProperty("Settings")]
        [JsonConverter(typeof(StringAllToListJsonConverter))]
        public List<string> Settings { get; set; }

        /// <summary>
        /// Adds permissions to access the Content Delivery.
        /// </summary>
        [JsonProperty("ContentDelivery")]
        [JsonConverter(typeof(StringAllToListJsonConverter))]
        public List<string> ContentDelivery { get; set; }
    }
}
