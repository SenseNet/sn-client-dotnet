# sensenet Client library for .Net

[![NuGet](https://img.shields.io/nuget/v/SenseNet.Client.svg)](https://www.nuget.org/packages/SenseNet.Client)

This component lets you work with the [sensenet](https://github.com/SenseNet/sensenet) Content Repository (create or manage content, execute queries, etc.) by providing a **C# client API** for the main content operations.

This library connects to a sensenet **REST API**, but **hides the underlying HTTP requests**. You can work with simple load or create Content operations in C#, instead of having to construct web requests yourself.
````csharp
var content = await Content.LoadAsync(id);
````
The component exposes a completely *async API* so that you can use it in a resource-friendly way.

The client *Content* class gives you a *dynamic* object so you can access fields (that are essentially properties in the response JSON) easily.
````csharp
dynamic content = await Content.LoadAsync(id);
DateTime date = content.BirthDate;
````
Querying or uploading is also made easy.
````csharp
await Content.UploadAsync(parentId, fileName, stream);
var results = await Content.QueryAsync(queryText);
````

*This article can also be read in [Brazilian Portuguese](README-pt-BR.md).*

# sensenet as a service (SNaaS) - use sensenet from the cloud

For a monthly subscription fee, we store all your content and data, relieving you of all maintenance-related tasks and installation, ensuring easy onboarding, easy updates, and patches.

https://www.sensenet.com/pricing
