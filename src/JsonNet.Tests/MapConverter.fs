module Newtonsoft.Json.FSharp.Tests.MapConverter

open Swensen.Unquote
open Fuchu
open Newtonsoft.Json
open Newtonsoft.Json.FSharp

open System

module NestedModule =
  type PropWithMap =
    { b : Map<string, string> }

  type ThisFails =
    { a : string * string
      m : Map<string, string> }

  type ThisWorks =
    { y : Map<string, string>
      z : string * string }

  type ThirdAttempt =
    { t : string * string
      s : string }

open NestedModule

[<Tests>]
let mapTests =
  let mapSer o      = JsonConvert.SerializeObject(o, Serialisation.converters |> Array.ofList)
  let mapDeser aStr = JsonConvert.DeserializeObject<Map<string, int>>(aStr, Serialisation.converters |> Array.ofList)

  testList "map tests" [
    testCase "baseline: serialising empty map {} to string (defaults)" <| fun _ ->
      let js = mapSer Map.empty<string, int>
      js =? "{}"

    testCase "serialising empty map {} to string" <| fun _ ->
      (mapSer Map.empty<string, int>) =? "{}"

    testCase "deserialising {} to Map.empty<string, int>" <| fun _ ->
      (mapDeser "{}") =? Map.empty<string, int>

    testCase @"deserialising { ""a"": 3 } to map [ ""a"" => 3 ]" <| fun _ ->
      let res = JsonConvert.DeserializeObject<Map<string, int>>("""{ "a": 3 }""", MapConverter())
      res =? ([("a", 3)] |> Map.ofList)

    testCase "serialising empty map roundtrip" <| fun _ ->
      test [MapConverter()] Map.empty

    testCase "serialising nonempty map roundtrip" <| fun _ ->
      test [MapConverter()] ([("a", 1); ("b", 2); ("c", 3)] |> Map.ofList)

    testCase "prop with map" <| fun () ->
      test Serialisation.converters { b = Map.empty }

    testCase "prop with map (explicit)" <| fun () ->
      let res = deserialise<PropWithMap> Serialisation.converters
                                  """{"b":{}}"""
      Assert.Equal("should be eq to res", { b = Map.empty }, res)

    testCase "failing test - array before object" <| fun _ ->
      let res = deserialise<ThisFails> Serialisation.converters
                                       """{"a":["xyz","zyx"],"m":{}}"""
      Assert.Equal("should be eq to res", { a = "xyz", "zyx"; m = Map.empty }, res)

    testCase "passing test - array after object" <| fun _ ->
      let res = deserialise<ThisWorks> Serialisation.converters
                                       """{"y":{},"z":["xyz","zyx"]}"""
      Assert.Equal("should be eq to res", { y = Map.empty; z = "xyz", "zyx" }, res)

    testCase "string after tuple" <| fun _ ->
      let res = deserialise<ThirdAttempt> Serialisation.converters
                                          """{"t":["",""],"s":"xyz"}"""
      Assert.Equal("should be eq to res", { t = "", ""; s = "xyz" }, res)


    //testProp "playing with map alias" <| fun (dto : NestedModule.ADto) ->
    //  test Serialisation.converters dto
    ]
