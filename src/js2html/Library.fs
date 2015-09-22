namespace NICE.js2html

open System
open System.IO
open DotLiquid
open FSharp.Data
open Nessos.Argu
open FSharp.Control
open FSharpx.Control
open System.Text.RegularExpressions
module Renderer =
    let (++) a b = System.IO.Path.Combine(a,b)

    /// Represents a local file system relative to the specified 'root'
    let private localFileSystem root =
      { new DotLiquid.FileSystems.IFileSystem with
          member this.ReadTemplateFile(context, templateName) =
            let templatePath = context.[templateName] :?> string
            let fullPath = Path.Combine(root, templatePath + ".liquid")
            if not (File.Exists(fullPath)) then failwithf "File not found: %s" fullPath
            File.ReadAllText(fullPath) }

    /// Protects accesses to various DotLiquid internal things
    let private safe =
      let o = obj()
      fun f -> lock o f

    // JsonValue to Hash
    let rec private asModel (target: JsonValue) =
        target.Properties()
            |> Seq.map(fun (key, value) -> (key, value |> asModelValue :> obj ) )
            |> dict
            |> Hash.FromDictionary

    and private asModelValue (value: JsonValue) =
        match value.GetType().Name with
         | "String" ->   value.AsString() :> obj
         | "Array" ->    value.AsArray() |> Seq.map asModelValue :> obj
         | "Record" ->   value |> asModel :> obj
         | _ -> value.AsString() :> obj

    /// Use the ruby naming convention by default
    do Template.NamingConvention <- DotLiquid.NamingConventions.RubyNamingConvention()


    /// loads a file
    let private fromFile (fileName:string) = async {
            use! file = File.AsyncOpenText fileName
            return file.ReadToEnd()
        }
    /// writes a file
    let private toFile fileName (contents:string) = async {
        File.AsyncWriteAllText (fileName,contents) |> Async.Start
        return ()
       }
    // -------------------------------------------------------------------------------------------------
    // Parsing and loading DotLiquid templates
    // -------------------------------------------------------------------------------------------------

    let mutable private templatesDir = None
    let parsedTemplates = System.Collections.Concurrent.ConcurrentDictionary<_,_>()
    /// loads a template
    let private renderAs (templateType: string) =
        let templateFile = templateType.ToLower()
        let templatePath =
            match templatesDir with
            | None -> Path.Combine( ".",templateFile )
            | Some root -> Path.Combine( root, templateFile )
        let t = parsedTemplates.GetOrAdd (templatePath,Template.Parse( (fromFile templatePath |> Async.RunSynchronously  )))
        fun v -> t.Render( v |> asModel )


    let setTemplatesDir dir =
        if templatesDir <> Some dir then
            templatesDir <- Some dir
            safe (fun () -> Template.FileSystem <- localFileSystem dir)

    let private generateFromJson modelType modelJson =
        JsonValue.Parse modelJson |> renderAs modelType

    let private generateFromFile modelType inputFile = async {
      let! input = fromFile inputFile
      return (generateFromJson modelType input,
              Path.GetFileNameWithoutExtension(inputFile) + ".html" )
      }

    // if matched, return (command name, command value) as a tuple
    let (|Command|_|) s =
      let r = new Regex(@"^(?:-{1,2}|\/)(?<command>\w+)[=:]*(?<value>.*)$",RegexOptions.IgnoreCase)
      let m = r.Match(s)
      if m.Success
        then
          Some(m.Groups.["command"].Value.ToLower(), m.Groups.["value"].Value)
        else
          None

    let parse args =
      args
      |> Seq.map (fun i ->
                    match i with
                    | Command (n,v) -> (n,v) // command
                    | _ -> ("",i)            // data
                  )
      |> Seq.scan (fun (sn,_) (n,v) -> if n.Length>0 then (n,v) else (sn,v)) ("","")
      |> Seq.skip 1
      |> Seq.groupBy (fun (n,_) -> n)
      |> Seq.map (fun (n,s) -> (n, s |> Seq.map (fun (_,v) -> v) |> Seq.filter (fun i -> i.Length>0)))
      |> Map.ofSeq

    [<EntryPoint>]
    let main args =
        let args = parse args
        let scalar (arg,def) =
          if args.ContainsKey arg then
            args.[arg] |> Seq.head
          else def
        let list arg =
          if args.ContainsKey arg then args.[arg] else Seq.empty
        let templates = scalar ("templatedir","./views")
        setTemplatesDir templates

        let template = scalar ("template","")
        let output = scalar ("output",".")
        let apply f = async{
                  let! (text,fn) = generateFromFile template f
                  toFile (output ++ fn) text |> Async.Start
                  return fn
                  }

        printfn "Loading templates from %s, using root %s" templates template

        printfn "%A" (args)
        match scalar ("json",""),
              list "file" with
            | "",xs when Seq.isEmpty xs   -> failwithf "--json or --file is required"
            | "",xs ->
              AsyncSeq.ofSeq xs
              |> AsyncSeq.map apply
              |> AsyncSeq.iter (fun s -> (printfn "%s" (Async.RunSynchronously s)))
              |> Async.RunSynchronously
            | json,_  ->
                generateFromJson template json |> Console.Write
        0 // return an integer exit code
