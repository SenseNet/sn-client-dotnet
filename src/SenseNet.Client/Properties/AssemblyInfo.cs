using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("SenseNet.Client.Tests")]

#if DEBUG
[assembly: AssemblyTitle("SenseNet.Client (Debug)")]
#else
[assembly: AssemblyTitle("SenseNet.Client (Release)")]
#endif

[assembly: AssemblyDescription("Client library for sensenet")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Sense/Net Inc.")]
[assembly: AssemblyProduct("SenseNet.Client")]
[assembly: AssemblyCopyright("Copyright © Sense/Net Inc.")]
[assembly: AssemblyTrademark("Sense/Net Inc.")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("1.1.1.0")]
[assembly: AssemblyFileVersion("1.1.1.0")]
[assembly: AssemblyInformationalVersion("1.1.1")]

[assembly: ComVisible(false)]
[assembly: Guid("d42c2274-9265-4db9-9f3e-845002f009c7")]