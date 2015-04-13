module Newtonsoft.Json.FSharp.Tests.TypeNaming

open Fuchu
open Swensen.Unquote

open Newtonsoft.Json
open Newtonsoft.Json.FSharp

type A = | B | C

module Internally =

  type I = I of string

[<Tests>]
let type_naming_tests =
  testList "type naming" [

    testCase "class in ordinary namespace" <| fun _ ->
      let name = TypeNaming.nameObj 2I
      name =? "urn:System.Numerics:BigInteger"
    
    testCase "union nested type (DeclaringType is not null)" <| fun _ ->
      let name = TypeNaming.nameObj (B)
      name =? "urn:Newtonsoft.Json.FSharp.Tests:TypeNaming_A|B"

    testCase "union nested type 2 (DeclaringType is not null)" <| fun _ ->
      let name = TypeNaming.nameObj (Newtonsoft.Json.FSharp.Tests.Principal.X)
      name =? "urn:Newtonsoft.Json.FSharp.Tests:Principal_Cmd|X"

    testCase "union nested and module-nested type (DeclaringType is not null)" <| fun _ ->
      let name = TypeNaming.nameObj (Internally.I "greet the world")
      name =? "urn:Newtonsoft.Json.FSharp.Tests:TypeNaming_Internally_I|I"

    testCase "union type in namespace (DeclaringType is null)" <| fun _ ->
      let name = TypeNaming.nameObj (D)
      name =? "urn:Newtonsoft.Json.FSharp.Tests:Outer|D"

    testCase "union type in namespace (DeclaringType is union)" <| fun _ ->
      let name = TypeNaming.nameObj (H)
      name =? "urn:Newtonsoft.Json.FSharp.Tests:Outer2Inner|H"

    ]

let urn = "urn:Example.Dtos:A|ABC"

[<Tests>]
let type_naming_parse_tests =

  let urn = "urn:Example.Dtos:A|ABC"

  testList "type naming parse tests" [
    testCase "parse type case name" <| fun _ ->
      "ABC" =? ((fun n -> n.CaseName.Value) << TypeNaming.parse) urn

    testCase "parse type name" <| fun _ ->
      "A" =? ((fun n -> n.Name) << TypeNaming.parse) urn

    testCase "parse type namespace" <| fun _ ->
      "Example.Dtos" =? ((fun n -> n.Namespace) << TypeNaming.parse) urn

    testCase "parse type case name" <| fun _ ->
      let urn = "urn:Example.Dtos:A|ABC"
      "ABC" =? ((fun n -> n.CaseName.Value) << TypeNaming.parse) urn

    ]