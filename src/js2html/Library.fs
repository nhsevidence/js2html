namespace NICE.js2html

open System
open System.IO
open DotLiquid
open FSharp.Data
open Nessos.Argu

module Renderer =

    // -------------------------------------------------------------------------------------------------
    // Registering things with DotLiquid
    // -------------------------------------------------------------------------------------------------

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
        let valueType = value.GetType().Name
        match valueType with
        | "String" ->   value.AsString() :> obj
        | "Array" ->    (value.AsArray() |> Seq.map asModelValue ) :> obj
        | "Record" ->   (value |> asModel) :> obj
        | _ ->
            let item = value.AsString()
            printfn "\t--> %s" valueType
            item :> obj

    /// Use the ruby naming convention by default
    do Template.NamingConvention <- DotLiquid.NamingConventions.RubyNamingConvention()

    // file helpers

    /// loads a file
    let private fromFile (fileName:string) =
        use file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        use reader = new StreamReader( file )
        reader.ReadToEnd()

    /// writes a file
    let private toFile (fileName:string) (contents:string) =
        use file = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite)
        use writer = new StreamWriter(file)
        writer.Write contents

    // -------------------------------------------------------------------------------------------------
    // Parsing and loading DotLiquid templates
    // -------------------------------------------------------------------------------------------------

    let mutable private templatesDir = None

    /// loads a template
    let private renderAs (templateType: string) =
        let templateFile = templateType.ToLower() + ".liquid"
        let templatePath =
            match templatesDir with
            | None -> Path.Combine( ".",templateFile )
            | Some root -> Path.Combine( root, templateFile )
        let t = Template.Parse( fromFile templatePath )
        fun v -> t.Render( v |> asModel )

    /// loads a template
    let private fromModelJson (modelJson: string) =
        JsonValue.Parse( modelJson )

    let private fromModelFile (modelPath: string) =
        fromFile modelPath

    /// loads a template
    let private outputAs (outputPath: string) (contents:string) =
        toFile outputPath contents

    // -------------------------------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------------------------------

    /// Set the root directory where DotLiquid is looking for templates. For example, you can
    /// write something like this:
    ///
    ///     DotLiquid.setTemplatesDir (__SOURCE_DIRECTORY__ + "/templates")
    ///
    /// note: The current directory is a global variable. This is a DotLiquid limitation.
    let setTemplatesDir dir =
        if templatesDir <> Some dir then
            templatesDir <- Some dir
            safe (fun () -> Template.FileSystem <- localFileSystem dir)

    /// Render a page using DotLiquid template. Takes a path (relative to the directory specified
    /// using `setTemplatesDir` and a value that is exposed as the "model" variable. You can use
    /// any F# record type, seq<_> and list<_> without having to explicitly register the fields.
    ///
    ///     let app = Renderer.parse "drugModel.json"
    ///
    let private generateFromJson (modelType:string) (modelJson:string) =
        fromModelJson modelJson |> renderAs modelType

    /// Render a page using DotLiquid template. Takes a path (relative to the directory specified
    /// using `setTemplatesDir` and a value that is exposed as the "model" variable. You can use
    /// any F# record type, seq<_> and list<_> without having to explicitly register the fields.
    ///
    ///     let app = Renderer.parse "drugModel.json"
    ///
    let private generateFromFile (modelType:string) (inputFile:string) =
        fromModelFile inputFile |> generateFromJson modelType

    type Arguments =
    | [<Mandatory>] TemplateName of string
    | ModelFile of string
    | ModelJson of string
    | OutputFile of string
    | TemplateDir of string
    with
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | TemplateName _ -> "The name of the view to use to render the passed json object"
                | ModelFile _ -> "Location of a JSON file to load as the JSON object"
                | ModelJson _ -> "a JSON string to use as the JSON object"
                | OutputFile _ -> "Optional location to output the generated HTML"
                | TemplateDir _ -> "Change the loation of the view templates used from ./views/"

    let parser = ArgumentParser.Create<Arguments>()

    // get usage text
    let usage = parser.Usage()

    [<EntryPoint>]
    let main args =
        let args = parser.Parse args
        setTemplatesDir((args.GetResult (<@ TemplateDir @>, "./views/" )) )

        // mandatory
        let TemplateName = (args.GetResult (<@ TemplateName @> ))

        let html =
          match (args.GetResult (<@ ModelJson @>, "" ))
                (args.GetResult (<@ ModelFile @>, "" )) with
            | "",""   -> failwithf "--modeljson or --modelfile is required"
            | "",file -> generateFromFile TemplateName file
            | json,_  -> generateFromJson TemplateName json


        match (args.GetResult (<@ OutputFile @>, "" )) with
            | "" -> print html
            | output -> html  |> outputAs output

        0 // return an integer exit code
