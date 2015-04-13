module Newtonsoft.Json.FSharp.Tests.TupleArrayConverter
open Fuchu
open Swensen.Unquote

open System
open Newtonsoft.Json
open Newtonsoft.Json.FSharp

type TupleAlias = float * int

[<Tests>]
let tests =
  testCase "serialising tuple" <| fun _ ->
    test Serialisation.converters (0., 23 : TupleAlias)