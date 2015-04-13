module Newtonsoft.Json.FSharp.Tests.Properties

open System
open Fuchu
open FsCheck
open Newtonsoft.Json
open Newtonsoft.Json.FSharp
open Newtonsoft.Json.FSharp.Tests

type B =
  | D1 of string * int
  | D2 of E
  | D3 of Map<string, int>
  | D4 of D * int
  | D5 of int * D
and D =
  | E1 of string
  | E2 of float * string * int * D
and E = { lhs : B; rhs : string }

let roundtrip a =
  let serialised = JsonConvert.SerializeObject(a, Formatting.Indented, Serialisation.converters |> List.toArray)
  JsonConvert.DeserializeObject(serialised, Serialisation.converters |> List.toArray)

[<Tests>]
let properties =
  testList "properties" [
    testProp "serialising any" (fun (a : B) -> test Serialisation.converters a)
  ]