using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if !NETSTANDARD
[assembly: AssemblyTitle("NsqSharp")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Jud White")]
[assembly: AssemblyProduct("NsqSharp")]
[assembly: AssemblyVersion("1.0.0")]
// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("db037231-b5c0-4084-b560-d4ff471b2aff")]
#endif
[assembly: AssemblyDescription("A .NET client library for NSQ, a realtime distributed messaging platform.")]
[assembly: AssemblyCopyright("Copyright © Jud White 2015-2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: InternalsVisibleTo("NsqSharp.Tests, PublicKey=" +
                              "00240000048000009400000006020000002400005253413100040000010001007f27c2fd8be864" +
                              "3851813c7df74f3a885801c1809f6e22f0eeaa245407988d6c2e54bceb9cf1092e8b3933f41aa1" +
                              "cca3c79d4022df3462bb7fb4cf4b4408ba1b5b705754d4c265c40ea7ffeee1825dc6bdb2722d98" +
                              "77536e60f80d019c9258916d3678ad9dc0961408a09dc8080ede07425cb1b5478e82fe7597939f" +
                              "c186f5f1")]

[assembly: InternalsVisibleTo("NsqSharp.WindowsHosting, PublicKey=" +
                              "00240000048000009400000006020000002400005253413100040000010001007f27c2fd8be864" +
                              "3851813c7df74f3a885801c1809f6e22f0eeaa245407988d6c2e54bceb9cf1092e8b3933f41aa1" +
                              "cca3c79d4022df3462bb7fb4cf4b4408ba1b5b705754d4c265c40ea7ffeee1825dc6bdb2722d98" +
                              "77536e60f80d019c9258916d3678ad9dc0961408a09dc8080ede07425cb1b5478e82fe7597939f" +
                              "c186f5f1")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]



// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
