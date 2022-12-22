﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace VirtoCommerce.Contentful.Models.Management
{
    /// <summary>
    /// Represents the details of a webhook call.
    /// </summary>
    public class WebHookCallDetails : IContentfulResource
    {
        /// <summary>
        /// Common system managed metadata properties.
        /// </summary>
        [JsonProperty("sys", NullValueHandling = NullValueHandling.Ignore)]
        public SystemProperties SystemProperties { get; set; }

        /// <summary>
        /// The http status code of the call.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// The errors that occurred, if any.
        /// </summary>
        public List<string> Errors { get; set; }

        /// <summary>
        /// The type of event that triggered the webhook
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// The url that was called.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The date and time the request was made.
        /// </summary>
        public DateTime? RequestAt { get; set; }

        /// <summary>
        /// The date and time the response was received.
        /// </summary>
        public DateTime? ResponseAt { get; set; }

        /// <summary>
        /// Details of the response.
        /// </summary>
        public WebHookResponse Response { get; set; }

        /// <summary>
        /// Details of the request.
        /// </summary>
        public WebHookRequest Request { get; set; }
    }
}
