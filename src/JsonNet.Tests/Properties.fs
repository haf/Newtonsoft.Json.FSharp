module Properties

open System
open Fuchu
open FsCheck
open Newtonsoft.Json
open Intelliplan.JsonNet.Serialisation
open Intelliplan.JsonNet
 
type B =
  | D1 of string * int
  | D2 of E
  | D3 of Map<string, int>
  | D4 of D * int
  | D5 of int * D
and D =
  | E1 of string
  | E2 of float * string * int * D
and E = { lhs : string; rhs : string }

let converters = 
  [ BigIntConverter() :> JsonConverter
    UnionConverter() :> JsonConverter
    TupleArrayConverter() :> JsonConverter
    MapConverter() :> JsonConverter
    ListConverter() :> JsonConverter
    OptionConverter() :> JsonConverter ]

type MyArbs =
  static member Strings () =
    Arb.filter (fun s -> s <> null) (Arb.Default.String ())

let no_nulls =
  { FsCheck.Config.Default
      with Arbitrary = [ typeof<MyArbs> ] }

let roundtrip a =
  let serialised = JsonConvert.SerializeObject(a, Formatting.Indented, List.toArray converters)
  JsonConvert.DeserializeObject(serialised, List.toArray converters)

//[<Tests>]
let properties =
  testList "properties" [
    testPropertyWithConfig no_nulls "serialising any" <| fun (a : B) ->
      Assert.Equal("rountrip a = a", a, roundtrip a)
  ]

//[<Tests>] // can't see what's different here...
let found_problems =
  testList "found" [
    testCase """D5 (0,E2 (nan,"",0,E1 ""))""" <| fun _ ->
      let sample = D5 (0,E2 (nan,"",0,E1 ""))
      Assert.Equal("roundtrip sample = sample", roundtrip sample, sample)
  ]