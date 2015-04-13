module Newtonsoft.Json.FSharp.Tests.FSharpRecord

open Fuchu
open Newtonsoft.Json
open Newtonsoft.Json.FSharp

type Dto =
  { hello  : int
    world  : string
    values : Map<string, string> }

type MyRecord =
  { lhs : string
    rhs : string }

let test a = test Serialisation.converters a

[<Tests>]
let typeNamingTests =
  testList "simple dto" [
    testCase "simple record" <| fun _ ->
      test { lhs = "2 3 e"
             rhs = "245abc" }

    testCase "record with empty map" <| fun _ ->
      test { hello = 45
             world = "trap"
             values = Map.empty }

    testCase "record with non-empty map" <| fun _ ->
      test { hello = 46
             world = "universe"
             values = [ "item1", string 35; "item2", string 567 ] |> Map.ofList }
  ]