﻿using System.Collections.Generic;

namespace VirtoCommerce.Contentful.Models.Management
{
    /// <summary>
    /// Represents a request to a web hook.
    /// </summary>
    public class WebHookRequest
    {
        /// <summary>
        /// The url this request called.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The headers that were sent with the request.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// The body of the request.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// The http method that the request was called with.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// The configured timeout of the request.
        /// </summary>
        public int Timeout { get; set; }
    }
}
