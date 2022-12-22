﻿using System.Collections.Generic;

namespace VirtoCommerce.Contentful.Models.Management
{
    /// <summary>
    /// Represents a response from a web hook.
    /// </summary>
    public class WebHookResponse
    {
        /// <summary>
        /// The url the response came from.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The headers and cookies the response included.
        /// </summary>
        public Dictionary<string, dynamic> Headers { get; set; }

        /// <summary>
        /// The body of the response.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// The status code returned with the response.
        /// </summary>
        public int StatusCode { get; set; }
    }
}
