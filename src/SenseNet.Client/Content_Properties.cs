using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace SenseNet.Client;

//[AttributeUsage(AttributeTargets.Property)]
//public class ReferenceFieldAttribute : Attribute
//{

//}

public class Binary
{
    [JsonProperty(PropertyName = "edit_media")]
    public string EditMedia { get; set; }
    [JsonProperty(PropertyName = "media_src")]
    public string MediaSrc { get; set; }
    [JsonProperty(PropertyName = "content_type")]
    public string ContentType { get; set; }
    [JsonProperty(PropertyName = "media_etag")]
    public string MediaEtag { get; set; }
}

public partial class Content
{
    private void SetProperties(dynamic responseContent)
    {
        foreach (var property in this.GetType().GetProperties())
        {
            var jsonValue = responseContent[property.Name];

            if (TryConvertToProperty(property.Name, jsonValue, out object propertyValue))
            {
                //UNDONE: handle error: type mismatch in conversion.
                property.SetMethod.Invoke(this, new[] { propertyValue });
                continue;
            }

            if (jsonValue == null)
                continue;
            if ((jsonValue is JObject) && jsonValue["__deferred"] != null)
                continue;
            var propertyType = property.PropertyType;

            if (propertyType == typeof(int) || propertyType == typeof(int?))
            {
                if (jsonValue is JValue jValue)
                    property.SetMethod.Invoke(this, new object[] { jValue.Value<int>() });
                continue;
            }
            if (propertyType == typeof(bool) || propertyType == typeof(bool?))
            {
                if (jsonValue is JValue jValue)
                    property.SetMethod.Invoke(this, new object[] { jValue.Value<bool>() });
                continue;
            }
            if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
            {
                if (jsonValue is JValue jValue)
                    property.SetMethod.Invoke(this, new object[] { jValue.Value<DateTime>() });
                continue;
            }
            if (propertyType == typeof(long) || propertyType == typeof(long?))
            {
                if (jsonValue is JValue jValue)
                    property.SetMethod.Invoke(this, new object[] { jValue.Value<long>() });
                continue;
            }
            if (propertyType == typeof(decimal) || propertyType == typeof(decimal?))
            {
                if (jsonValue is JValue jValue)
                    property.SetMethod.Invoke(this, new object[] { jValue.Value<decimal>() });
                continue;
            }
            if (propertyType == typeof(double) || propertyType == typeof(double?))
            {
                if (jsonValue is JValue jValue)
                    property.SetMethod.Invoke(this, new object[] { jValue.Value<double>() });
                continue;
            }
            if (propertyType == typeof(float) || propertyType == typeof(float?))
            {
                if (jsonValue is JValue jValue)
                    property.SetMethod.Invoke(this, new object[] { jValue.Value<float>() });
                continue;
            }
            if (propertyType == typeof(Binary))
            {
                if (jsonValue is JObject jObject)
                    property.SetMethod.Invoke(this, new object[] { jObject["__mediaresource"].ToObject<Binary>() });
                continue;
            }
            if (propertyType == typeof(string))
            {
                if (jsonValue is JValue jValue)
                    property.SetMethod.Invoke(this, new object[] { jValue.Value<string>() });
                if (jsonValue is JArray jArray)
                    property.SetMethod.Invoke(this, new object[] { jArray[0].Value<string>() });
                continue;
            }

            if (propertyType == typeof(string[]))
            {
                if (jsonValue is JValue jValue)
                    property.SetMethod.Invoke(this, new object[] {new[] {jValue.Value<string>()}});
                if (jsonValue is JArray jArray)
                {
                    property.SetMethod.Invoke(this,
                        jArray.Count == 0
                            ? new object[] {Array.Empty<string>()}
                            : new object[] {jArray.ToObject<string[]>()});
                }
                continue;
            }
            if (propertyType == typeof(int[]))
            {
                if (jsonValue is JValue jValue)
                    property.SetMethod.Invoke(this, new object[] { new[] { jValue.Value<int>() } });
                if (jsonValue is JArray jArray)
                {
                    property.SetMethod.Invoke(this,
                        jArray.Count == 0
                            ? new object[] { Array.Empty<int>() }
                            : new object[] { jArray.ToObject<int[]>() });
                }
                continue;
            }
            //UNDONE: Are there more supported int and string enumerable types?


            //if (property.GetCustomAttribute<ReferenceFieldAttribute>() == null)

            if (typeof(Content).IsAssignableFrom(propertyType))
            {
                // Single reference
                var referredContent = GetReference(jsonValue, propertyType);
                property.SetMethod.Invoke(this, new[] { referredContent });
                continue;
            }

            if (propertyType.IsArray)
            {
                // Multi reference to a content array
                var itemType = propertyType.GetElementType();
                if (typeof(Content).IsAssignableFrom(itemType))
                {
                    var referredContents = GetMultiReferenceArray(jsonValue, itemType);
                    property.SetMethod.Invoke(this, new[] { referredContents });
                    continue;
                }
            }

            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                // Multi reference to an IEnumerable<?>
                var itemType = propertyType.GetGenericArguments().First();
                if (typeof(Content).IsAssignableFrom(itemType))
                {
                    var referredContents = GetMultiReferenceArray(jsonValue, itemType);
                    property.SetMethod.Invoke(this, new[] { referredContents });
                    continue;
                }
            }

            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                // Multi reference to a List<?>
                var itemType = propertyType.GetGenericArguments().First();
                if (typeof(Content).IsAssignableFrom(itemType))
                {
                    var referredContents = GetMultiReferenceList(jsonValue, itemType);
                    property.SetMethod.Invoke(this, new[] { referredContents });
                    continue;
                }
            }

            if (jsonValue is JObject customObject)
            {
                property.SetMethod.Invoke(this, new[] {customObject.ToObject(propertyType)});
                continue;
            }

            //UNDONE: ? set default instead of throw exception.
            throw new NotImplementedException("#1");
        }
    }

    protected virtual bool TryConvertToProperty(string propertyName, JToken jsonValue, out object propertyValue)
    {
        propertyValue = null;
        return false;
    }
    //UNDONE: Convert back custom values.
    protected virtual bool TryConvertFromProperty(string propertyName, out object convertedValue)
    {
        convertedValue = null;
        return false;
    }

    private Array GetMultiReferenceArray(object jsonValue, Type itemType)
    {
        Content[] referredContents = GetReferences(jsonValue, itemType);
        if (referredContents == null)
            return null;
        
        var array = Array.CreateInstance(itemType, referredContents.Length);

        for (int i = 0; i < referredContents.Length; i++)
            array.SetValue(referredContents[i], i);

        return array;
    }
    private IList GetMultiReferenceList(object jsonValue, Type itemType)
    {
        Content[] referredContents = GetReferences(jsonValue, itemType);
        if (referredContents == null)
            return null;

        var listType = typeof(List<>);
        var constructedListType = listType.MakeGenericType(itemType);
        var list = (IList)Activator.CreateInstance(constructedListType);

        foreach (var content in referredContents)
            list.Add(content);

        return list;
    }

    private Content GetReference(object input, Type propertyType)
    {
        if (input == null)
            return null;
        if (input is JValue jValue)
        {
            if (jValue.Type == JTokenType.Null)
                return null;
            throw new NotSupportedException($"GetReference failed. Token type {jValue.Type} is not supported.");
        }
        if (input is JObject jObject)
        {
            // jObject["__deferred"]["uri"].Value<string>()

            var content = Repository.CreateContentFromJson(jObject, propertyType);
            return content;
        }
        if (input is JArray jArray)
        {
            return GetReference(jArray[0], propertyType);
        }
        throw new NotSupportedException($"GetReference failed. Object type {input.GetType().FullName} is not supported.");
    }
    private Content[] GetReferences(object input, Type itemType)
    {
        if (input == null)
            return null;
        if (input is JValue jValue)
        {
            if (jValue.Type == JTokenType.Null)
                return null;
            throw new NotSupportedException($"GetReferences failed. Token type {jValue.Type} is not supported.");
        }
        if (input is JObject jObject)
        {
            // jObject["__deferred"]["uri"].Value<string>()
            var content = Repository.CreateContentFromJson(jObject);
            return new[] { content };
        }
        if (input is JArray jArray)
        {
            var contents = jArray.Select(x => GetReference(x, itemType)).ToArray();
            return contents;
        }
        throw new NotSupportedException($"GetReferences failed. Object type {input.GetType().FullName} is not supported.");
    }


    private void ManagePostData(IDictionary<string, object> postData)
    {
        foreach (var property in this.GetType().GetProperties())
        {
            if (property.Name == "FieldNames")
                continue;
            if (property.Name == "Id")
                continue;
            if (property.Name == "Item")
                continue;
            if (property.Name == "Path")
                continue;
            if(property.Name == "ParentPath")
                continue;
            if (property.Name == "Repository")
                continue;
            if (property.Name == "Server")
                continue;
            if (property.Name == "ParentId")
                continue;

            var propertyValue = property.GetGetMethod().Invoke(this, null);
            //UNDONE: Convert back custom values.


            postData[property.Name] = propertyValue;
        }
    }

}