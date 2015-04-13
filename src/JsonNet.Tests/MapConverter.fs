module Newtonsoft.Json.FSharp.Tests.MapConverter

open Swensen.Unquote
open Fuchu
open Newtonsoft.Json
open Newtonsoft.Json.FSharp

open System

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
    ]
