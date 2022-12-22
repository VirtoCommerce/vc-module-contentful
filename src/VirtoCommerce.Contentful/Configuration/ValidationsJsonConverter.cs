﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using VirtoCommerce.Contentful.Extensions;
using VirtoCommerce.Contentful.Models.Management;
using VirtoCommerce.Contentful.Search;

namespace VirtoCommerce.Contentful.Configuration
{
    /// <summary>
    /// JsonConverter for converting <see cref="VirtoCommerce.Contentful.Models.Management.IFieldValidator"/>.
    /// </summary>
    public class ValidationsJsonConverter : JsonConverter
    {
        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">The type to convert to.</param>
        public override bool CanConvert(Type objectType)
        {
            return objectType is IFieldValidator;
        }

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
            var jsonObject = JObject.Load(reader);

            JToken jToken;
            if (jsonObject.TryGetValue("size", out jToken))
            {
                return new SizeValidator(
                    jToken["min"].ToNullableInt(),
                    jToken["max"].ToNullableInt(),
                    jsonObject["message"]?.ToString());
            }

            if (jsonObject.TryGetValue("range", out jToken))
            {
                return new RangeValidator(
                    jToken["min"].ToNullableInt(),
                    jToken["max"].ToNullableInt(),
                    jsonObject["message"]?.ToString());
            }

            if (jsonObject.TryGetValue("in", out jToken))
            {
                return new InValuesValidator(jToken.Values<string>(), jsonObject["message"]?.ToString());
            }

            if (jsonObject.TryGetValue("linkMimetypeGroup", out jToken))
            {
                if (jToken is JValue)
                {
                    //single string value returned for mime type field. This seems to be an inconsistency in the API that needs to be handled.

                    var type = jToken.Value<string>();
                    return new MimeTypeValidator(new[] { (MimeTypeRestriction)Enum.Parse(typeof(MimeTypeRestriction), type, true) },
                    jsonObject["message"]?.ToString());
                }

                var types = jToken.Values<string>();
                return new MimeTypeValidator(types.Select(c => (MimeTypeRestriction)Enum.Parse(typeof(MimeTypeRestriction), c, true)),
                    jsonObject["message"]?.ToString());
            }

            if (jsonObject.TryGetValue("linkContentType", out jToken))
            {
                return new LinkContentTypeValidator(jToken.Values<string>(), jsonObject["message"]?.ToString());
            }

            if (jsonObject.TryGetValue("regexp", out jToken))
            {
                return new RegexValidator(jToken["pattern"]?.ToString(), jToken["flags"]?.ToString(), jsonObject["message"]?.ToString());
            }

            if (jsonObject.TryGetValue("unique", out jToken))
            {
                return new UniqueValidator();
            }

            if (jsonObject.TryGetValue("dateRange", out jToken))
            {
                return new DateRangeValidator(
                    jToken["min"]?.ToString(),
                    jToken["max"]?.ToString(),
                    jsonObject["message"]?.ToString());
            }

            if (jsonObject.TryGetValue("assetFileSize", out jToken))
            {
                return new FileSizeValidator(
                    jToken["min"].ToNullableInt(),
                    jToken["max"].ToNullableInt(),
                    SystemFileSizeUnits.Bytes,
                    SystemFileSizeUnits.Bytes,
                    jsonObject["message"]?.ToString());
            }

            if (jsonObject.TryGetValue("assetImageDimensions", out jToken))
            {
                int? minWidth = null;
                int? maxWidth = null;
                int? minHeight = null;
                int? maxHeight = null;
                if (jToken["width"] != null)
                {
                    var width = jToken["width"];
                    minWidth = width["min"].ToNullableInt();
                    maxWidth = width["max"].ToNullableInt();
                }
                if (jToken["height"] != null)
                {
                    var height = jToken["height"];
                    minHeight = height["min"].ToNullableInt();
                    maxHeight = height["max"].ToNullableInt();
                }
                return new ImageSizeValidator(minWidth, maxWidth, minHeight, maxHeight, jsonObject["message"]?.ToString());
            }

            return Activator.CreateInstance(objectType);
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, (value as IFieldValidator).CreateValidator());
        }
    }
}
