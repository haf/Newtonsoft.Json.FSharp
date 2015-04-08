module Program

open Fuchu
open Newtonsoft.Json.FSharp.Logging
open System.Diagnostics
open NodaTime

type DebugPrinter (name : string) =
  interface Newtonsoft.Json.FSharp.Logging.Logger with
    member x.Verbose f_line =
      Debug.WriteLine (sprintf "%s: %A" name (f_line ()))
    member x.Debug f_line =
      Debug.WriteLine (sprintf "%s: %A" name (f_line ()))
    member x.Log line =
      Debug.WriteLine (sprintf "%s: %A" name line)
      
[<EntryPoint>]
let main argv =
  Newtonsoft.Json.FSharp.Logging.configure
    (SystemClock.Instance)
    (fun name -> DebugPrinter name :> Logger)

  //Tests.run Newtonsoft.Json.FSharp.Tests.MapTests.map_tests
  defaultMainThisAssembly argv