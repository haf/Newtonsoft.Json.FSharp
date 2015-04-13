module Newtonsoft.Json.FSharp.Tests.UriConverter
open Fuchu

open System

open Newtonsoft.Json
open Newtonsoft.Json.FSharp

[<Tests>]
let tests =
  testList "uri tests" [
    testCase "converting uri" <| fun () ->
      test Serialisation.converters (Uri "https://haf.se")
    //testProp "converting uri" <| fun (u : Uri) ->
    //  test Serialisation.converters u
  ]