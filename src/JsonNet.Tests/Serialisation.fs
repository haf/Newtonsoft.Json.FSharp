module Intelliplan.JsonNet.Tests.Serialisation

open Fuchu

open Intelliplan.JsonNet

type HelloWorld =
  | HelloWorld of string * int

[<Tests>]
let tests =
  testList "can serialise normally" [
    testCase "simple string" <| fun _ ->
      Serialisation.serialise' "hi" |> ignore

    testCase "simple sum type" <| fun _ ->
      let name, data =
        Serialisation.serialise' (HelloWorld ("hi", 12345))

      Assert.Equal("should have data", true, data.Length > 0)
    ]