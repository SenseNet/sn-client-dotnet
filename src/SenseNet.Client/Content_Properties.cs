﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace SenseNet.Client;

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

    private readonly string[] _skippedProperties = new[]
        {"FieldNames", "Id", "Item", "ParentPath", "ParentId", "Path", "Repository", "Server"};
    private void ManagePostData(IDictionary<string, object> postData)
    {
        var originalFields = (JObject)_responseContent;

        foreach (var property in this.GetType().GetProperties())
        {
            if (_skippedProperties.Contains(property.Name))
                continue;

            if (!TryConvertFromProperty(property.Name, out var propertyValue))
            {
                propertyValue = property.GetGetMethod().Invoke(this, null);
                propertyValue = ConvertFromReferredContents(property.Name, property.PropertyType, propertyValue);
            }

            if (originalFields != null)
            {
                if (originalFields.TryGetValue(property.Name, out var originalValue))
                {
                    if (ManageReferences(property.PropertyType, property.Name, propertyValue, originalValue, postData))
                        continue;
                    var originalRawValue = originalValue is JObject ? JsonHelper.Serialize(originalValue) : originalValue.ToString();
                    var currentRawValue = propertyValue is string ? (string)propertyValue : JsonHelper.Serialize(propertyValue);
                    if (currentRawValue == originalRawValue)
                        continue;
                }
            }

            postData[property.Name] = propertyValue;
        }
    }

    private object ConvertFromReferredContents(string propertyName, Type propertyType, object propertyValue)
    {
        if (typeof(Content).IsAssignableFrom(propertyType))
        {
            // Single reference
            var referredContent = (Content) propertyValue;
            if (referredContent == null)
                return null;
            if (referredContent.Id > 0)
                return new[] { referredContent.Id };
            else if (referredContent.Path != null)
                return new[] { referredContent.Path };
            throw new ApplicationException(
                "Reference cannot be recognized. The referred content should have the Id or Path.");
        }

        if (propertyType.IsArray)
        {
            // Multi reference to a content array
            var itemType = propertyType.GetElementType();
            if (typeof(Content).IsAssignableFrom(itemType))
            {
                throw new NotImplementedException();
                var referredContents = propertyValue as IEnumerable<Content>;
                // manage by ids or paths
                return true;
            }
        }

        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            // Multi reference to an IEnumerable<?>
            var itemType = propertyType.GetGenericArguments().First();
            if (typeof(Content).IsAssignableFrom(itemType))
            {
                throw new NotImplementedException();
            }
        }

        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
        {
            // Multi reference to a List<?>
            var itemType = propertyType.GetGenericArguments().First();
            if (typeof(Content).IsAssignableFrom(itemType))
            {
                var result = new List<object>();
                if (propertyValue != null)
                {
                    foreach (var referredContent in (IEnumerable<Content>) propertyValue)
                    {
                        if (referredContent == null)
                            continue;
                        if (referredContent.Id > 0)
                        {
                            result.Add(referredContent.Id);
                            continue;
                        }
                        if (referredContent.Path != null)
                        {
                            result.Add(referredContent.Path);
                            continue;
                        }
                        throw new ApplicationException("One or more referred cannot be recognized." +
                                                       $" The referred content should have the Id or Path. FieldName: '{propertyName}'.");
                    }
                }

                return result.ToArray();
            }
        }
        return propertyValue;
    }

    private bool ManageReferences(Type propertyType, string propertyName, object propertyValue, JToken originalValue, IDictionary<string, object> postData)
    {
        if (typeof(Content).IsAssignableFrom(propertyType))
        {
            // Single reference
            var originalReferredContent = GetReference(originalValue, propertyType);
            //UNDONE: check reference diff by ids or paths
            postData[propertyName] = propertyValue;
            return true;
        }

        if (propertyType.IsArray)
        {
            // Multi reference to a content array
            var itemType = propertyType.GetElementType();
            if (typeof(Content).IsAssignableFrom(itemType))
            {
                var originalReferredContents = GetMultiReferenceArray(originalValue, itemType);
                //UNDONE: check reference diff by ids or paths
                postData[propertyName] = propertyValue;
                return true;
            }
        }

        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            // Multi reference to an IEnumerable<?>
            var itemType = propertyType.GetGenericArguments().First();
            if (typeof(Content).IsAssignableFrom(itemType))
            {
                var originalReferredContents = GetMultiReferenceArray(originalValue, itemType);
                //UNDONE: check reference diff by ids or paths
                postData[propertyName] = propertyValue;
                return true;
            }
        }

        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
        {
            // Multi reference to a List<?>
            var itemType = propertyType.GetGenericArguments().First();
            if (typeof(Content).IsAssignableFrom(itemType))
            {
                var originalReferredContents = GetMultiReferenceArray(originalValue, itemType);
                //UNDONE: check reference diff by ids or paths
                postData[propertyName] = propertyValue;
                return true;
            }
        }

        return false;
    }
}