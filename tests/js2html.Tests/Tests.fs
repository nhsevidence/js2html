module js2html.Tests


open NUnit.Framework
open NICE.js2html

[<Test>]
let ``convert single file`` () =
  let r = Renderer.main [|"--file";"../../examples/1.json"
                          "--template";"test.html"
                          "--templatedir";"../../examples/views/"
                          "--output";"../../expected/" |]
  ()
[<Test>]
let ``convert json string`` () =
  let r = Renderer.main [|"--json";"""{"hello":"world"}"""
                          "--template";"test.html"
                          "--templatedir";"../../examples/views" |]
  ()

[<Test>]
let ``convert multiple files`` () =
  let file = "--file"::([1..2] |> List.map (fun i -> sprintf "../../examples/%d.json" i))
  let r = Renderer.main (Array.ofList (file @
                                       ["--template";"test.html"
                                        "--templatedir";"../../examples/views"
                                        "--output";"../../expected/"]))
  ()
