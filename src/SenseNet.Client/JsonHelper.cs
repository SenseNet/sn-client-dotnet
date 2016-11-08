using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SenseNet.Client
{
    /// <summary>
    /// Provides helper methods for JSON operations.
    /// </summary>
    public static class JsonHelper
    {
        //============================================================================= Properties

        /// <summary>
        /// Default serializer settings with ISO date format and None as type name handling option.
        /// </summary>
        public static JsonSerializerSettings JsonSerializerSettings { get; } = new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            TypeNameHandling = TypeNameHandling.None
        };

        //============================================================================= Static API

        /// <summary>
        /// Serializes the specified object as a JSON string.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        public static string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj, JsonHelper.JsonSerializerSettings);
        }
        /// <summary>
        /// Deserializes the JSON to the specified .NET type.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
        /// <param name="json">The JSON to deserialize.</param>
        /// <returns>The deserialized object from the JSON string.</returns>
        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, JsonHelper.JsonSerializerSettings);
        }
        /// <summary>
        /// Deserializes the JSON to a dynamic JObject.
        /// </summary>
        /// <param name="json">The JSON to deserialize.</param>
        /// <returns>A dynamic JObject deserialized from the JSON string.</returns>
        public static dynamic Deserialize(string json)
        {
            return JsonConvert.DeserializeObject(json, JsonHelper.JsonSerializerSettings) as JObject;
        }

        /// <summary>
        /// Serializes a .NET object to JSON and wraps it into a 'models=[...]' array
        /// that can be sent to the OData REST API.
        /// </summary>
        /// <param name="data">A .NET object to serialize.</param>
        public static string GetJsonPostModel(object data)
        {
            return "models=[" + Serialize(data) + "]";
        }
    }
}
