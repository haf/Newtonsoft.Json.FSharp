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

[<Tests>]
let any =
  testPropertyWithConfig no_nulls "serialising any" <| fun (a : B) ->
    let serialised = JsonConvert.SerializeObject(a, Formatting.Indented, List.toArray converters)
    let deserialised = JsonConvert.DeserializeObject(serialised, List.toArray converters)
    deserialised = a