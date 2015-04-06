#light
namespace FirstLine
open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
[<assembly: AssemblyCompany("Company")>]
[<assembly: AssemblyProduct("auth")>]
[<assembly: AssemblyName("Name")>]
[<assembly: AssemblyCopyright("Copyright")>]
[<assembly: AssemblyTrademark("Trademark ™")>]
[<assembly: AssemblyDescription("Description")>]
#if DEBUG
[<assembly: AssemblyConfiguration("Debug")>]
#else
[<assembly: AssemblyConfiguration("Release")>]
#endif
[<assembly: ComVisible(false)>]
[<assembly: AssemblyVersion("1.0.0.0")>]
[<assembly: AssemblyFileVersion("1.0.0.0")>]
// Instructs log4net to read its configuration from AppName.exe.config file
[<assembly: log4net.Config.XmlConfigurator(Watch = true)>]
()
