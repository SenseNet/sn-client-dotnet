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
    public static void AssertParameterTypeIsContentInFunction<T>(this Repository repository, [CallerMemberName] string caller = null)
    {
        if (typeof(Content).IsAssignableFrom(typeof(T)))
            return;

        // :) We will consider your request but ...
        throw new System.ApplicationException($"The {caller} cannot be called with type parameter {typeof(T).FullName}. " +
                                              "If the type parameter is not the Content or any inherited type, call the " +
                                              "InvokeFunctionAsync<T>");
    }
    public static void AssertParameterTypeIsNotContentInFunction<T>(this Repository repository, [CallerMemberName] string caller = null)
    {
        if (!typeof(Content).IsAssignableFrom(typeof(T)))
            return;

        throw new System.ApplicationException($"The {caller} cannot be called with type parameter {typeof(T).FullName}. " +
                                              "If the type parameter is Content or any inherited type, call the " +
                                              "InvokeContentFunctionAsync<T> or InvokeContentCollectionFunctionAsync<T> method.");
    }
    public static void AssertParameterTypeIsContentInAction<T>(this Repository repository, [CallerMemberName] string caller = null)
    {
        if (!typeof(Content).IsAssignableFrom(typeof(T)))
            return;

        throw new System.ApplicationException($"The {caller} cannot be called with type parameter {typeof(T).FullName}. " +
                                              $"If the type parameter is not Content or any inherited type, call the " +
                                              $"InvokeActionAsync<T>.");
    }
    public static void AssertParameterTypeIsNotContentInAction<T>(this Repository repository, [CallerMemberName] string caller = null)
    {
        if (!typeof(Content).IsAssignableFrom(typeof(T)))
            return;

        throw new System.ApplicationException($"The {caller} cannot be called with type parameter {typeof(T).FullName}. " +
                                              $"If the type parameter is Content or any inherited type, call the " +
                                              $"InvokeContentActionAsync<T> or InvokeContentCollectionActionAsync<T> method.");
    }

    public static T ProcessOperationResponse<T>(this Repository repository, string response)
    {
        if (string.IsNullOrEmpty(response))
            return default;
        repository.AssertParameterTypeIsNotContentInFunction<T>();
        return JsonConvert.DeserializeObject<T>(response);
    }
    public static T ProcessContentOperationResponse<T>(this Repository repository, string response) where T : Content
    {
        if (string.IsNullOrEmpty(response))
            return default;
        repository.AssertParameterTypeIsContentInFunction<T>();

        dynamic json = JsonConvert.DeserializeObject(response);

        return (T)BuildContentFromResponse(repository, json.d, typeof(T));
    }
    public static IContentCollection<T> ProcessContentCollectionOperationResponse<T>(this Repository repository, string response) where T : Content
    {
        if (string.IsNullOrEmpty(response))
            return ContentCollection<T>.Empty;
        repository.AssertParameterTypeIsContentInFunction<T>();

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