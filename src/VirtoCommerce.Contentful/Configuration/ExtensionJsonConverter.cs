﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using VirtoCommerce.Contentful.Models;
using VirtoCommerce.Contentful.Models.Management;

namespace VirtoCommerce.Contentful.Configuration
{
    /// <summary>
    /// JsonConverter for converting <see cref="VirtoCommerce.Contentful.Models.Management.UiExtension"/>.
    /// </summary>
    public class ExtensionJsonConverter : JsonConverter
    {
        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">The type to convert to.</param>
        public override bool CanConvert(Type objectType) => objectType == typeof(UiExtension);

        /// <summary>
        /// Gets a value indicating whether this JsonConverter can write JSON.
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The reader to use.</param>
        /// <param name="objectType">The object type to serialize into.</param>
        /// <param name="existingValue">The current value of the property.</param>
        /// <param name="serializer">The serializer to use.</param>
        /// <returns>The deserialized <see cref="Asset"/>.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jObject = JObject.Load(reader);
            var extensionProperties = jObject.SelectToken("$.extension");

            var extension = new UiExtension();

            extension.SystemProperties = jObject["sys"].ToObject<SystemProperties>();
            extension.Src = extensionProperties["src"]?.ToString();
            extension.Name = extensionProperties["name"]?.ToString();
            extension.Sidebar = extensionProperties["sidebar"]?.Value<bool>() ?? false;
            extension.FieldTypes = extensionProperties["fieldTypes"]?.Values<dynamic>()?.Select(c => c.type.ToString())?.Cast<string>().ToList();
            extension.SrcDoc = extensionProperties["srcDoc"]?.ToString();

            return extension;
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var extension = value as UiExtension;

            if (extension == null)
            {
                return;
            }

            serializer.Serialize(writer,
                new
                {
                    sys = extension.SystemProperties,
                    extension = new
                    {
                        src = extension.Src,
                        name = extension.Name,
                        fieldTypes = extension.FieldTypes,
                        srcDoc = extension.SrcDoc,
                        sidebar = extension.Sidebar
                    }
                });
        }
    }
}
