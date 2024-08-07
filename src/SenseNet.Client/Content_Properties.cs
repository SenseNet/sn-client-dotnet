﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace SenseNet.Client;

public partial class Content
{
    private void SetProperties(dynamic responseContent)
    {
        foreach (var property in this.GetType()
                     .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.CanRead && p.CanWrite))
        {
            var jsonValue = responseContent[property.Name];

            if (TryConvertToProperty(property.Name, jsonValue, out object propertyValue))
            {
                try
                {
                    property.SetMethod.Invoke(this, new[] { propertyValue });
                }
                catch (Exception e)
                {
                    throw new ApplicationException($"The property '{property.Name}' cannot be set. " +
                                                   $"See inner exception for details.", e);
                }
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
                {
                    var binary = jObject["__mediaresource"].ToObject<Binary>();
                    binary.OwnerContent = this;
                    property.SetMethod.Invoke(this, new object[] { binary });
                }
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
            //TODO: Are there more supported int and string enumerable types?

            // Handle reference fields
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

            if (TryParseToEnumValue(property, jsonValue, out propertyValue))
            {
                try
                {
                    property.SetMethod.Invoke(this, new[] { propertyValue });
                }
                catch (Exception e)
                {
                    throw new ApplicationException($"The property '{property.Name}' cannot be set. " +
                                                   $"See inner exception for details.", e);
                }
                continue;
            }

            // General object 
            if (jsonValue is JObject customObject)
            {
                property.SetMethod.Invoke(this, new[] {customObject.ToObject(propertyType)});
            }

            // Property could not be bound: do nothing.
        }
    }

    protected virtual bool TryConvertToProperty(string propertyName, JToken jsonValue, out object propertyValue)
    {
        switch (propertyName)
        {
            case nameof(VersioningMode):
            case nameof(InheritableVersioningMode):
            {
                if (StringArrayToInt(jsonValue, out var converted))
                    propertyValue = (VersioningMode)converted;
                else
                    propertyValue = null;
                return true;
            }
            case nameof(ApprovingMode):
            case nameof(InheritableApprovingMode):
            {
                if (StringArrayToInt(jsonValue, out var converted))
                    propertyValue = (ApprovingEnabled)converted;
                else
                    propertyValue = null;
                return true;
            }
            default:
                propertyValue = null;
                return false;
        }
    }
    protected bool StringArrayToInt(JToken jsonValue, out int converted)
    {
        var stringValue = ((jsonValue as JArray)?.FirstOrDefault() as JValue)?.Value<string>();
        return int.TryParse(stringValue, out converted);
    }
    protected bool StringArrayToEnum<TEnum>(JToken jsonValue, out object propertyValue) where TEnum : struct
    {
        var arrayValue = jsonValue as JArray;
        if (arrayValue != null && arrayValue.Count == 0)
        {
            propertyValue = null;
            return true;
        }

        TEnum parsed;
        var stringValue = (arrayValue?.FirstOrDefault() as JValue)?.Value<string>();
        if (Enum.TryParse(stringValue, true, out parsed))
        {
            propertyValue = parsed;
            return true;
        }
        propertyValue = null;
        return false;
    }
    private bool TryParseToEnumValue(PropertyInfo property, JToken jsonValue, out object propertyValue)
    {
        propertyValue = null;
        var pType = property.PropertyType;

        if (pType.IsEnum)
        {
            propertyValue = ConvertToEnum(pType, jsonValue);
            return true;
        }

        if (!pType.IsGenericType)
            return false;
        if (pType.Name != "Nullable`1")
            return false;
        pType = pType.GetGenericArguments()[0];
        if (!pType.IsEnum)
            return false;

        propertyValue = ConvertToEnum(pType, jsonValue);
        return true;
    }

    private object ConvertToEnum(Type enumType, JToken jsonValue)
    {
        var inputValues = GetStringValuesFromJsonArray(enumType, jsonValue);
        if (inputValues == null || inputValues.Length == 0)
            return null;

        var names = Enum.GetNames(enumType);
        var values = new object[names.Length];
        Enum.GetValues(enumType).CopyTo(values, 0);
        int[] intValues;
        try
        {
            intValues = values.Select(Convert.ToInt32).ToArray();
        }
        catch(Exception e)
        {
            throw new ClientException($"Unsupported enum type: {enumType.FullName}.", e);
        }

        var combinedValue = 0;
        for (var i = 0; i < names.Length; i++)
        {
            var valueName = GetEnumNameFromValue(enumType, values[i]);
            if (inputValues.Contains(valueName))
                combinedValue |= intValues[i];
        }

        return Enum.ToObject(enumType, combinedValue);
    }
    private string[] GetStringValuesFromJsonArray(Type enumType, JToken jToken)
    {
        if (jToken == null)
            return Array.Empty<string>();
        var jArray = (JArray) jToken;
        var values = jArray.Select(x=>x.ToString()).ToArray();
        if (enumType.GetCustomAttributes<FlagsAttribute>().Any() && values.Length > 1)
            return values;
        return values.Take(1).ToArray();
    }

    protected virtual bool TryConvertFromProperty(string propertyName, out object convertedValue)
    {
        switch (propertyName)
        {
            case nameof(VersioningMode):
                convertedValue = EnumValueToStringArray((int?)VersioningMode);
                return true;
            case nameof(InheritableVersioningMode):
                convertedValue = EnumValueToStringArray((int?)InheritableVersioningMode);
                return true;
            case nameof(ApprovingMode):
                convertedValue = EnumValueToStringArray((int?)ApprovingMode);
                return true;
            case nameof(InheritableApprovingMode):
                convertedValue = EnumValueToStringArray((int?)InheritableApprovingMode);
                return true;
            default:
                convertedValue = null;
                return false;
        }
    }
    protected string[] EnumValueToStringArray(int? propertyValue)
    {
        return propertyValue == null ? null : new[] { propertyValue.ToString() };
    }
    protected string[] EnumNameToStringArray(string enumName)
    {
        if (string.IsNullOrEmpty(enumName))
            return null;
        return new[] { enumName };
    }


    private Array GetMultiReferenceArray(object jsonValue, Type itemType)
    {
        Content[] referredContents = GetReferences(jsonValue, itemType);
        if (referredContents == null)
            return null;
        
        var array = Array.CreateInstance(itemType, referredContents.Length);

        for (int i = 0; i < referredContents.Length; i++)
        {
            if (itemType.IsAssignableFrom(referredContents[i].GetType()))
            {
                array.SetValue(referredContents[i], i);
                continue;
            }
            if (referredContents.Length == 1 && referredContents[0].FieldNames.SingleOrDefault() == "__deferred")
            {
                return null; //Array.CreateInstance(itemType, 0);
            }
            if (referredContents.Length > 1)
            {
                throw new ClientException($"Cannot  convert {referredContents[i].GetType().FullName} to {itemType.FullName}");
            }
        }

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
            return jArray.Count == 0 ? null : GetReference(jArray[0], propertyType);
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
        {"FieldNames", "Id", "Item", "ParentPath", "ParentId", "Path", "Versions", "Repository", "Server"};
    private void ManagePostData(IDictionary<string, object> postData)
    {
        var originalFields = (JObject)_responseContent;

        foreach (var property in this.GetType().GetProperties().Where(p=>!IsIgnored(p)))
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
                    var originalRawValue = JsonHelper.Serialize(originalValue).Trim('"');
                    if (originalRawValue == "[]")
                        originalRawValue = "null";
                    var currentRawValue = propertyValue is string ? (string)propertyValue : JsonHelper.Serialize(propertyValue).Trim('"');
                    if (currentRawValue == originalRawValue)
                        continue;
                }
                else
                {
                    if (propertyValue == null)
                        continue;
                }
            }
            else
            {
                if (propertyValue == null)
                    continue;
            }

            postData[property.Name] = propertyValue;
        }
    }

    private readonly Type[] _ignoreAttributeTypes = {
        typeof(JsonIgnoreAttribute), typeof(System.Text.Json.Serialization.JsonIgnoreAttribute)
    };

    private bool IsIgnored(PropertyInfo property)
    {
        return property.GetCustomAttributes()
            .Select(a => a.GetType())
            .Any(t => _ignoreAttributeTypes.Contains(t));
    }

    private object ConvertFromReferredContents(string propertyName, Type propertyType, object propertyValue)
    {
        if (propertyValue == null)
            return null;

        if (typeof(Content).IsAssignableFrom(propertyType))
        {
            // Single reference
            var referredContent = (Content) propertyValue;
            if (referredContent.Id > 0)
                return new[] { referredContent.Id };
            if (referredContent.Path != null)
                return new[] { referredContent.Path };
            throw CreateReferenceCannotBeRecognizedException(propertyName);
        }

        if (propertyType.IsArray)
        {
            // Multi reference to a content array
            var itemType = propertyType.GetElementType();
            if (typeof(Content).IsAssignableFrom(itemType))
            {
                var items = ConvertReferencesToRequestValue((IEnumerable<Content>)propertyValue, propertyName, true);
                return items.Length == 0 ? null : items;
            }
        }

        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            // Multi reference to an IEnumerable<?>
            var itemType = propertyType.GetGenericArguments().First();
            if (typeof(Content).IsAssignableFrom(itemType))
            {
                var items = ConvertReferencesToRequestValue((IEnumerable<Content>)propertyValue, propertyName, true);
                return items.Length == 0 ? null : items;
            }
        }

        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
        {
            // Multi reference to a List<?>
            var itemType = propertyType.GetGenericArguments().First();
            if (typeof(Content).IsAssignableFrom(itemType))
            {
                var items = ConvertReferencesToRequestValue((IEnumerable<Content>)propertyValue, propertyName, true);
                return items.Length == 0 ? null : items;
            }
        }

        if(propertyType.IsEnum)
        {
            return ConvertFromEnum(propertyType, propertyValue);
        }
        if (propertyType.IsGenericType)
        {
            var genericArg = propertyType.GetGenericArguments()[0];
            if (genericArg.IsEnum)
                return ConvertFromEnum(genericArg, propertyValue);
        }

        return propertyValue;
    }

    private object ConvertFromEnum(Type propertyType, object propertyValue)
    {
        var isFlagsEnum = propertyType.GetCustomAttributes<FlagsAttribute>().Any();
        if (!isFlagsEnum)
            return new[] { GetEnumNameFromValue(propertyType, propertyValue) };

        var intValue = Convert.ToInt32(propertyValue);
        var resultValues = new List<string>();
        foreach (var enumValue in Enum.GetValues(propertyType))
        {
            if ((intValue & Convert.ToInt32(enumValue)) == 0)
                continue;
            var result = GetEnumNameFromValue(propertyType, enumValue);
            resultValues.Add(result);
        }
        return resultValues.ToArray();
    }

    private string GetEnumNameFromValue(Type enumType, object enumValue)
    {
        var result = enumValue.ToString();
        var members = enumType.GetMember(result);
        var member = members.FirstOrDefault(m => m.DeclaringType == enumType);
        if (member == null)
            return null;
        var valueAttribute = (JsonPropertyAttribute)member
            .GetCustomAttributes(typeof(JsonPropertyAttribute), false)
            .FirstOrDefault();
        var valueName = valueAttribute?.PropertyName ?? result;
        return valueName;
    }

    private object[] ConvertReferencesToRequestValue(IEnumerable<Content> source, string propertyName, bool throwOnError)
    {
        var result = new List<object>();

        // The source cannot be null
        foreach (var content in source)
        {
            var item = ConvertReferenceToRequestValue(content, propertyName, throwOnError);
            if(item != null)
                result.Add(item);
        }
        return result.ToArray();
    }

    private object ConvertReferenceToRequestValue(Content content, string propertyName, bool throwOnError)
    {
        if (content == null)
            return null;
        if (content.Id > 0)
            return content.Id;
        if (content.Path != null)
            return content.Path;
        if (!throwOnError)
            return null;
        throw CreateReferenceCannotBeRecognizedException(propertyName);
    }
    private Exception CreateReferenceCannotBeRecognizedException(string propertyName)
    {
        throw new ApplicationException("One or more referred content cannot be recognized." +
                                       $" The referred content should have the Id or Path. FieldName: '{propertyName}'.");
    }

    // Compares the serialized converted property value and original values and adds the property value
    // to the posted data if it is changed. It is also considered a change if the order of the elements has changed.
    private bool ManageReferences(Type propertyType, string propertyName, object propertyValue, JToken originalValue,
        IDictionary<string, object> postData)
    {
        if (typeof(Content).IsAssignableFrom(propertyType))
        {
            // Single reference
            if (MultiReferenceIsChanged(propertyValue, propertyName, originalValue, propertyType))
                postData[propertyName] = propertyValue;
            return true;

        }

        if (propertyType.IsArray)
        {
            // Multi reference to a content array
            var itemType = propertyType.GetElementType();
            if (typeof(Content).IsAssignableFrom(itemType))
            {
                if (MultiReferenceIsChanged(propertyValue, propertyName, originalValue, itemType))
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
                if (MultiReferenceIsChanged(propertyValue, propertyName, originalValue, itemType))
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
                if (MultiReferenceIsChanged(propertyValue, propertyName, originalValue, itemType))
                    postData[propertyName] = propertyValue;
                return true;
            }
        }

        return false;
    }
    private bool MultiReferenceIsChanged(object propertyValue, string propertyName,
        JToken originalValue, Type itemType)
    {
        var originalReferredContents = (IEnumerable<Content>) GetMultiReferenceArray(originalValue, itemType);
        if (originalReferredContents == null)
            return propertyValue != null;
        var originalRequest = ConvertReferencesToRequestValue(originalReferredContents, propertyName, false);
        if (originalRequest == null || propertyValue == null)
            return true;
        if (JsonHelper.Serialize(originalRequest) != JsonHelper.Serialize(propertyValue))
            return true;
        return false;
    }
}