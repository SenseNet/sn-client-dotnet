# sensenet Biblioteca cliente para .Net

[![Entre no chat em https://gitter.im/SenseNet/sn-client-dotnet](https://badges.gitter.im/SenseNet/sn-client-dotnet.svg)](https://gitter.im/SenseNet/sn-client-dotnet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![NuGet](https://img.shields.io/nuget/v/SenseNet.Client.svg)](https://www.nuget.org/packages/SenseNet.Client)

Esse componente permite que você trabalhe com o [sensenet](https://github.com/SenseNet/sensenet) Repositório de Conteúdo (crie ou gerencie conteúdo, execute consultas, etc.) fornecendo uma **C# API de cliente** para as principais operações de conteúdo.

Esta biblioteca se conecta a um sensenet **REST API**, mas **oculta as solicitações HTTP subjacentes**. Você pode trabalhar com carregamento simples ou criar operações de conteúdo em C#, em vez de ter que construir solicitações da Web por conta própria.
````csharp
var content = await Content.LoadAsync(id);
````
O componente expõe uma *API completamente assíncrona* para que você possa usá-lo de maneira amigável aos recursos.

A classe cliente *Content* fornece a você um objeto *dynamic* para que você possa acessar os campos (que são essencialmente propriedades na resposta JSON) facilmente.
````csharp
dynamic content = await Content.LoadAsync(id);
DateTime date = content.BirthDate;
````
Consulta ou upload também é fácil.
````csharp
await Content.UploadAsync(parentId, fileName, stream);
var results = await Content.QueryAsync(queryText);
````

Veja os detalhes e mais exemplos [aqui](http://wiki.sensenet.com/Client_library).

*Este artigo foi traduzido do [Inglês](README.md) e traduzido para [Português](README-pt-BR.md).*
