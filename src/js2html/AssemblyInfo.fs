namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("js2html")>]
[<assembly: AssemblyProductAttribute("js2html")>]
[<assembly: AssemblyDescriptionAttribute("Dotliquid templating for json objects")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"
