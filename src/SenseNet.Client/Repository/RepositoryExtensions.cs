using System;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using AngleSharp.Io;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

internal static class RepositoryExtensions
{
    public static void AssertParameterTypeIsContent<T>(this Repository repository, bool isAction, [CallerMemberName] string caller = null)
    {
        if (typeof(Content).IsAssignableFrom(typeof(T)))
            return;

        // :) We will consider your request but ...
        var msg = $"The {caller} cannot be called with type parameter {typeof(T).FullName}. " +
                  "If the type parameter is not the Content or any inherited type, call the " +
                  (isAction ? "InvokeActionAsync<T>" : "InvokeFunctionAsync<T>") + " method.";
        throw new System.ApplicationException(msg);
    }
    public static void AssertParameterTypeIsNotContent<T>(this Repository repository, bool isAction, [CallerMemberName] string caller = null)
    {
        if (!typeof(Content).IsAssignableFrom(typeof(T)))
            return;

        var msg = $"The {caller} cannot be called with type parameter {typeof(T).FullName}. " +
                  "If the type parameter is Content or any inherited type, call the " +
                  (isAction
                      ? "InvokeContentActionAsync<T> or InvokeContentCollectionActionAsync<T>"
                      : "InvokeContentFunctionAsync<T> or InvokeContentCollectionFunctionAsync<T>") +
                  " method."; ;
        throw new System.ApplicationException(msg);
    }

    public static T? ProcessOperationResponse<T>(this Repository repository, string response, bool isAction) //where T : class
    {
        if (string.IsNullOrEmpty(response))
            return default;
        repository.AssertParameterTypeIsNotContent<T>(isAction);
        if (typeof(T) == typeof(string))
            return (T)(object)response;
        if (typeof(T) == typeof(object))
            //return (T)(object)JsonConvert.DeserializeObject(response);
            return (T)DeserializeObjectOrString(response);
        return JsonConvert.DeserializeObject<T>(response);
    }

    private static object DeserializeObjectOrString(string response)
    {
        try
        {
            return JsonConvert.DeserializeObject(response) ?? string.Empty;
        }
        catch
        {
            // do nothing
        }
        return response;
    }
    public static T ProcessContentOperationResponse<T>(this Repository repository, string response, bool isAction) where T : Content
    {
        if (string.IsNullOrEmpty(response))
            return default;
        repository.AssertParameterTypeIsContent<T>(isAction);

        dynamic json = JsonConvert.DeserializeObject(response);

        return (T)BuildContentFromResponse(repository, json.d, typeof(T));
    }
    public static IContentCollection<T> ProcessContentCollectionOperationResponse<T>(this Repository repository, string response, bool isAction) where T : Content
    {
        if (string.IsNullOrEmpty(response))
            return ContentCollection<T>.Empty;
        repository.AssertParameterTypeIsContent<T>(isAction);

        var contents = BuildContentCollectionFromResponse<T>(repository, response);
        return contents;
    }


    private static Content BuildContentFromResponse(this Repository repository, JObject singleContentResponse, Type requestedType)
    {
        string contentTypeName = singleContentResponse["Type"]?.ToString();
        var contentType = repository.GetContentTypeByName(contentTypeName);

        Content content;
        try
        {
            content = (Content)repository.Services.GetRequiredService(contentType ?? requestedType);
        }
        catch (InvalidOperationException ex)
        {
            throw new ApplicationException("The content type is not registered: " + requestedType.Name, ex);
        }

        content.Server = repository.Server;
        content.Repository = repository;

        content.InitializeFromResponse(singleContentResponse);

        return content;
    }
    private static IContentCollection<T> BuildContentCollectionFromResponse<T>(this Repository repository, string response) where T : Content
    {
        var jsonResponse = JsonHelper.Deserialize(response);
        var totalCount = Convert.ToInt32(jsonResponse.d.__count ?? 0);
        var items = jsonResponse.d.results as JArray;
        var count = items?.Count ?? 0;
        var resultEnumerable = items?.Select(repository.CreateContentFromResponse<T>).ToArray() ?? Array.Empty<T>();
        return new ContentCollection<T>(resultEnumerable, count,
            totalCount);
    }

}