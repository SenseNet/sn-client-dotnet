using AngleSharp.Dom;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SenseNet.Client;

[AttributeUsage(AttributeTargets.Property)]
public class ReferenceFieldAttribute : Attribute
{

}

public partial class Content
{

    private void SetProperties(dynamic responseContent)
    {
        foreach (var property in this.GetType().GetProperties())
        {
            var jsonValue = responseContent[property.Name];
            if (jsonValue == null)
                continue;
            if ((jsonValue is JObject) && jsonValue["__deferred"] != null)
                continue;

            if (property.PropertyType == typeof(int))
            {
                if (jsonValue is JValue jValue)
                    property.SetMethod.Invoke(this, new object[] { jValue.Value<int>() });
                continue;
            }

            if (property.PropertyType == typeof(bool))
            {
                if (jsonValue is JValue jValue)
                    property.SetMethod.Invoke(this, new object[] { jValue.Value<bool>() });
                continue;
            }
            if (property.PropertyType == typeof(bool?))
            {
                if (jsonValue is JValue jValue)
                    property.SetMethod.Invoke(this, new object[] { jValue.Value<bool>() });
                continue;
            }
            if (property.PropertyType == typeof(DateTime))
            {
                if (jsonValue is JValue jValue)
                    property.SetMethod.Invoke(this, new object[] { jValue.Value<DateTime>() });
                continue;
            }
            if (property.PropertyType == typeof(DateTime?))
            {
                if (jsonValue is JValue jValue)
                    property.SetMethod.Invoke(this, new object[] { jValue.Value<DateTime>() });
                continue;
            }
            if (property.PropertyType == typeof(string))
            {
                if (jsonValue is JValue jValue)
                    property.SetMethod.Invoke(this, new object[] { jValue.Value<string>() });
                if (jsonValue is JArray jArray)
                    property.SetMethod.Invoke(this, new object[] { jArray[0].Value<string>() });
                continue;
            }
            if (property.PropertyType == typeof(string[]))
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
            if (property.PropertyType == typeof(int[]))
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

            //UNDONE: DataType.Currency
            //UNDONE: DataType.Binary




            //continue;

            if (property.GetCustomAttribute<ReferenceFieldAttribute>() == null)
    continue;
            var isEnumerable = IsEnumerableType(property.PropertyType, out var itemType);
            if (isEnumerable)
            {
                var referredContent = GetReferences(jsonValue, property.PropertyType, itemType);
                property.SetMethod.Invoke(this, new[] { referredContent });
            }
            else
            {
                var referredContent = GetReference(jsonValue, property.PropertyType);
                property.SetMethod.Invoke(this, new[] { referredContent });
            }
        }
    }

    private bool IsEnumerableType(Type type, out Type itemType)
    {
        // type.GetGenericTypeDefinition() == typeof(IEnumerable<>)

        itemType = null;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            // IEnumerable<>
            itemType = type.GetGenericArguments().First();
            return true;
        }

        var interfaceType = type
            .GetInterfaces()
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable));
        if (interfaceType != null)
        {
            // List<> ??
            itemType = interfaceType.GetGenericArguments().First();
            return true;
        }

        return false;
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
    private object GetReferences(object input, Type propertyType, Type itemType)
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

}