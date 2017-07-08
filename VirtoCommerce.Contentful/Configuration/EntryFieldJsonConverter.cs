﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Contentful.Core.Models;
using System.Linq;

namespace Contentful.Core.Configuration
{
    /// <summary>
    /// JsonConverter for converting entry fields into a cutomst object.
    /// </summary>
    [Obsolete("The EntryFieldJsonConverter no longer needs to be used and can be safely removed", true)]
    public class EntryFieldJsonConverter : JsonConverter
    {
        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">The type to convert to.</param>
        public override bool CanConvert(Type objectType) => true;
        private bool _canRead = true;

        /// <summary>
        /// Gets a value indicating whether this converter can currently read JSON.
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return _canRead;
            }
        }

        /// <summary>
        /// Whether or not this JsonConverter can write.
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The reader to use.</param>
        /// <param name="objectType">The object type to serialize into.</param>
        /// <param name="existingValue">The current value of the property.</param>
        /// <param name="serializer">The serializer to use.</param>
        /// <returns>The deserialized object.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jObject = JObject.Load(reader);
            var fields = jObject.SelectToken("$.fields");
            

            if (fields == null)
            {
                //We're here because the consumer is probably trying to serialize directly int objectType, just deserialize the jObject.
                _canRead = false;
                var jObectSerialized = jObject.ToObject(objectType);
                _canRead = true;
                return jObectSerialized;
            }

            //Important to set to false to make sure we don't try to use the JsonConverter again inside of ToObject
            _canRead = false;
            var returnObject = fields?.ToObject(objectType);
            _canRead = true;
            return returnObject;
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// **NOTE: This method is not implemented and will throw an exception.**
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
