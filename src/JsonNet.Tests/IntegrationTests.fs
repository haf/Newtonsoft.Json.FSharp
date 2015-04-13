module Newtonsoft.Json.FSharp.Tests.IntegrationTests

open System
open Swensen.Unquote
open Fuchu

open Newtonsoft.Json
open Newtonsoft.Json.FSharp

[<Tests>]
let expected_strings =
  testList "expected strings" [
    testCase "serialising list of ints" <| fun _ ->
      let settings =
        new JsonSerializerSettings()
        |> Newtonsoft.Json.FSharp.Serialisation.extend
      let str = JsonConvert.SerializeObject([1; 2; 3], settings)
      Assert.Equal("should have correct representation", "[1,2,3]", str)

  ]