module Program

open Fuchu
open Newtonsoft.Json.FSharp.Logging
open System.Diagnostics
open NodaTime

type DebugPrinter (name : string) =
  let print line =
    Debug.WriteLine ""
    Debug.WriteLine (sprintf "%s [%s]" line.message name)
    for x in line.data do
      Debug.Write (sprintf "  %s => %A" x.Key x.Value)
      Debug.WriteLine ""

  interface Newtonsoft.Json.FSharp.Logging.Logger with
    member x.Verbose f_line = print (f_line ())
    member x.Debug f_line = print (f_line ())
    member x.Log line = print line
      
[<EntryPoint>]
let main argv =
  Newtonsoft.Json.FSharp.Logging.configure
    (SystemClock.Instance)
    (fun name -> DebugPrinter name :> Logger)

  //let tests = Newtonsoft.Json.FSharp.Tests.MapConverter.mapTests
  //            |> Test.filter (fun s -> s.Contains("playing with empty id"))
  //Tests.run tests
  defaultMainThisAssembly argv