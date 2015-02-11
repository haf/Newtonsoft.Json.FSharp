namespace Newtonsoft.Json.FSharp.Tests

open System

open Newtonsoft.Json

open Swensen.Unquote

open Fuchu

open Newtonsoft.Json.FSharp
open Newtonsoft.Json.FSharp

type Outer = | D | E
type Outer2 = | F | G
and Outer2Inner = | H

module TypeNamingTests =

  type A = | B | C

  module Internally =

    type I = I of string

  [<Tests>]
  let type_naming_tests =
    testList "type naming tests" [

      testCase "class in ordinary namespace" <| fun _ ->
        let name = TypeNaming.nameObj 2I
        name =? "urn:System.Numerics:BigInteger"
      
      testCase "union nested type (DeclaringType is not null)" <| fun _ ->
        let name = TypeNaming.nameObj (B)
        name =? "urn:Newtonsoft.Json.FSharp.Tests:TypeNamingTests_A|B"

      testCase "union nested type 2 (DeclaringType is not null)" <| fun _ ->
        let name = TypeNaming.nameObj (logibit.Principals.Principal.X)
        name =? "urn:logibit.Principals:Principal_Cmd|X"

      testCase "union nested and module-nested type (DeclaringType is not null)" <| fun _ ->
        let name = TypeNaming.nameObj (Internally.I "greet the world")
        name =? "urn:Newtonsoft.Json.FSharp.Tests:TypeNamingTests_Internally_I|I"

      testCase "union type in namespace (DeclaringType is null)" <| fun _ ->
        let name = TypeNaming.nameObj (D)
        name =? "urn:Newtonsoft.Json.FSharp.Tests:Outer|D"

      testCase "union type in namespace (DeclaringType is union)" <| fun _ ->
        let name = TypeNaming.nameObj (H)
        name =? "urn:Newtonsoft.Json.FSharp.Tests:Outer2Inner|H"

      ]

module TypeNamingParseTests =

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

module SharedConverterContext =

  let test (serialisers : 'a list when 'a :> JsonConverter) a =
    let converters = serialisers |> List.map (fun x -> x :> JsonConverter) |> List.toArray
    let serialised = JsonConvert.SerializeObject(a, Formatting.Indented, converters)
    let deserialised = JsonConvert.DeserializeObject(serialised, converters)
    deserialised =? a

  let test' (serialiser : #JsonConverter) a =
    let serialisers = [ serialiser ]
    test serialisers a

module Fac =
  let bigIntConv = BigIntConverter()
  let unionConv  = UnionConverter()
  let tupleConv  = TupleArrayConverter()
  let mapConv    = MapConverter()

module BigIntConverterTests =
  // https://bugzilla.xamarin.com/show_bug.cgi?id=12233
  open SharedConverterContext

  // using the big int converter
  let test = test' Fac.bigIntConv

  [<Tests>]
  let tests =
    testList "can serialise BigInt" [
      testCase "serialising simple bigint" <| fun _ -> test 345I
      testCase "serialising negative bitint" <| fun _ -> test -234I
      testCase "serialising large bigint" <| fun _ -> test -1298398939838934983893893893893893893838389I ]

module RecordTests =
  open SharedConverterContext
  type MyRecord = { lhs : string; rhs : string }

  let test = test List.empty

  [<Tests>]
  let tests =
    testCase "serialising simple record" <| fun _ -> test { lhs = "2 3 e"; rhs = "245abc" }

module TupleTests =
  open SharedConverterContext
  type TupleAlias = float * int

  [<Tests>]
  let tests =
    testCase "serialising tuple" <| fun _ -> test [] (0., 23 : TupleAlias)

module MapTests =
  open SharedConverterContext
  // add to test to debug: System.Diagnostics.Debug.Listeners.Add(new System.Diagnostics.DefaultTraceListener());

  [<Tests>]
  let map_tests =
    let mapSer o      = JsonConvert.SerializeObject(o, MapConverter())
    let mapDeser aStr = JsonConvert.DeserializeObject<Map<string, int>>(aStr, MapConverter())

    testList "map tests" [

      testCase "baseline: serialising empty map {} to string (defaults)" <| fun _ ->
        let js = JsonConvert.SerializeObject(Map.empty<string, int>)
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
  
module UnionConverterTests =
  open SharedConverterContext

  let sampleStr = "{\r\n  \"_name\": \"urn:Newtonsoft.Json.FSharp.Tests:UnionConverterTests_Event|Created\",\r\n  \"Created\": null\r\n}"

  type Event = Created | Other

  type A =
    | D1 of string * int
    | D2 of B
    | D3 of C
    | D4 of C * int
    | D5 of int * C
  and B =
    | E1 of Guid
    | E2 of float * string * bigint * C
  and C = { lhs : string; rhs : string }

  module Inner =

    type Nabla =
      | N1

  let converters =
    [ Fac.bigIntConv :> JsonConverter
      Fac.unionConv :> JsonConverter
      Fac.tupleConv :> JsonConverter ]

  let test<'a when 'a : equality> : 'a -> unit = test converters

  [<Tests>]
  let union_converter_tests =
    testList "union converter tests" [

      testCase "serialising simple union" <| fun _ ->
        test <| D1("hello", -2)

      testCase "serialising simple nested union" <| fun _ ->
        test <| Inner.N1

      testCase "deserialising simple union" <| fun _ ->
        JsonConvert.DeserializeObject(sampleStr, typeof<Event>, Fac.unionConv)
        <>? null

      testCase "serialising union containing record" <| fun _ ->
        test <| D3({ lhs = "LHS" ; rhs = "RHS" })

      testCase "serialising union containing tuple containing record" <| fun _ ->
        test <| D4({ lhs = "LHS" ; rhs = "RHS" }, -23)

      testCase "serialising union containing tuple containing record after int" <| fun _ ->
        test <| D5(-43, { lhs = "LHS" ; rhs = "RHS" })

      testCase "serialising E2 - union containing tuple" <| fun _ ->
//        System.Diagnostics.Debug.Listeners.Add(new System.Diagnostics.DefaultTraceListener()) |> ignore
        test <| E2(0., "mystring", 2I, { lhs = "a"; rhs = "b" })

      testCase "serialising nested union" <| fun _ ->
        test <| D2(E1(Guid.Empty))

      testCase "serialising complex nested union" <| fun _ ->
        test <| D2(E2(-3.220000e03, "Goodbye World", 21345I, { lhs = "e"; rhs = "mail" }))

      ]