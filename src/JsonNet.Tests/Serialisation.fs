module Newtonsoft.Json.FSharp.Tests.Serialisation

open Fuchu

open Newtonsoft.Json.FSharp

type HelloWorld =
  | HelloWorld of string * int

[<Tests>]
let tests =
  testList "can serialise normally" [
    testCase "simple string" <| fun _ ->
      Serialisation.serialiseNoOpts "hi" |> ignore

    testCase "simple sum type" <| fun _ ->
      let sample = HelloWorld ("hi", 12345)
      let name, data =
        Serialisation.serialiseNoOpts sample

      Assert.Equal("should have data", true, data.Length > 0)

      let o = Serialisation.deserialiseNoOpts (typeof<HelloWorld>, data) :?> HelloWorld
      Assert.Equal("should eq structurally", sample, o)
    ]