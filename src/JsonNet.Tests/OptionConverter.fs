module Newtonsoft.Json.FSharp.Tests.OptionConverter

open Fuchu
open Swensen.Unquote

open System
open Newtonsoft.Json
open Newtonsoft.Json.FSharp

[<Tests>]
let expected_strings =
  let serialise (x : _ option) = 
    JsonConvert.SerializeObject(x, [| OptionConverter() :> JsonConverter |])
  let deserialise (str : string) =
    JsonConvert.DeserializeObject(str, [| OptionConverter() :> JsonConverter |])
    
  testList "for serialisation of option" [
    testCase "serialising option of int (None)" <| fun _ ->
      let str = serialise (None : int option)
      Assert.Equal("should have null representation", "null", str)

    testCase "serialising option of int (Some 2)" <| fun _ ->
      let str = serialise (Some 2)
      Assert.Equal("should have null representation", "2", str)

    testCase "deserialising option of int (2)" <| fun _ ->
      let res = deserialise "2" : int option
      Assert.Equal("should have (Some 2) representation", Some 2, res)

    testCase "deserialising option of int (null)" <| fun _ ->
      let res = deserialise "null" : int option
      Assert.Equal("should have (None) representation", None, res)
    ]