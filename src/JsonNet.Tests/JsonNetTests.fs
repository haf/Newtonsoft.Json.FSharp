namespace Intelliplan.JsonNet.Tests

open System
open Xunit
open Swensen.Unquote
open Newtonsoft.Json
open Intelliplan.JsonNet
open Intelliplan.JsonNet.TypeNaming
open Intelliplan.JsonNet.Serialisation

type FactNoMonoAttribute() as x =
  inherit FactAttribute()
  do if not <| (System.Type.GetType("Mono.Runtime") = null) then x.Skip <- "Ignored on mono"
//  member val Skip = "" with get, set

type Outer = | D | E
type Outer2 = | F | G
and Outer2Inner = | H

module TypeNamingTests =

  type A = | B | C

  module Internal =

    type I = I of string

  let [<FactNoMono>] ``class in ordinary namespace`` () =
    let name = TypeNaming.nameObj 2I
    name =? "urn:System.Numerics:BigInteger"
  
  let [<FactNoMono>] ``union nested type (DeclaringType is not null)`` () =
    let name = TypeNaming.nameObj (B)
    name =? "urn:Intelliplan.JsonNet.Tests:TypeNamingTests_A|B"

  let [<FactNoMono>] ``union type in namespace (DeclaringType is null)`` () =
    let name = TypeNaming.nameObj (D)
    name =? "urn:Intelliplan.JsonNet.Tests:Outer|D"

  let [<FactNoMono>] ``union type in namespace (DeclaringType is union)`` () =
    let name = TypeNaming.nameObj (H)
    name =? "urn:Intelliplan.JsonNet.Tests:Outer2Inner|H"

module TypeNamingParseTests =

  let urn = "urn:Intelliplan.Dtos:A|ABC"

  let [<FactNoMono>] ``parse type case name`` () =
    let urn = "urn:Intelliplan.Dtos:A|ABC"
    "ABC" =? ((fun n -> n.CaseName.Value) << TypeNaming.parse) urn

  let [<FactNoMono>] ``parse type name`` () =
    "A" =? ((fun n -> n.Name) << TypeNaming.parse) urn

  let [<FactNoMono>] ``parse type namespace`` () =
    "Intelliplan.Dtos" =? ((fun n -> n.Namespace) << TypeNaming.parse) urn

module SharedConverterContext =

  let test (serialisers : 'a list when 'a :> JsonConverter) a =
    let converters = serialisers |> List.map (fun x -> x :> JsonConverter) |> List.toArray
    let serialised = JsonConvert.SerializeObject(a, Formatting.Indented, converters)
//    Console.WriteLine(serialised)
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

  let [<FactNoMono>] ``serialising simple bitint`` () = test 345I
  let [<FactNoMono>] ``serialising negative bigint`` () = test -234I
  let [<FactNoMono>] ``serialising large bigint`` () = test -1298398939838934983893893893893893893838389I
  let [<FactNoMono(Skip = "need to build generator")>] ``serialising any`` () =
    let CanSerialiseAnyBigInteger (b : bigint) = test b
    FsCheck.Check.Quick CanSerialiseAnyBigInteger

module RecordTests =
  open SharedConverterContext
  type MyRecord = { lhs : string; rhs : string }

  let test = test List.empty
  
  let [<FactNoMono>] ``serialising simple record`` () = test { lhs = "2 3 e"; rhs = "245abc" }

module TupleTests =
  open SharedConverterContext
  type TupleAlias = float * int
  
  let [<FactNoMono>] ``serialising tuple`` () = test [] (0., 23 : TupleAlias)

module MapTests =
  open SharedConverterContext
  // add to test to debug: System.Diagnostics.Debug.Listeners.Add(new System.Diagnostics.DefaultTraceListener());

  let [<Fact>] ``baseline: serialising empty map {} to string (defaults)`` () =
    let js = JsonConvert.SerializeObject(Map.empty<string, int>)
    js =? "{}"

  let mapSer o      = JsonConvert.SerializeObject(o, MapConverter())
  let mapDeser aStr = JsonConvert.DeserializeObject<Map<string, int>>(aStr, MapConverter())

  let [<Fact>] ``serialising empty map {} to string`` () =
    (mapSer Map.empty<string, int>) =? "{}"

  let [<Fact>] ``deserialising {} to Map.empty<string, int>`` () =
    (mapDeser "{}") =? Map.empty<string, int>

  let [<Fact>] ``deserialising { "a": 3 } to map [ "a" => 3 ]`` () =
    let res = JsonConvert.DeserializeObject<Map<string, int>>("""{ "a": 3 }""", MapConverter())
    res =? ([("a", 3)] |> Map.ofList)
//
  let [<Fact>] ``serialising empty map roundtrip`` () = test [MapConverter()] Map.empty
  let [<Fact>] ``serialising nonempty map roundtrip`` () = test [MapConverter()] ([("a", 1); ("b", 2); ("c", 3)] |> Map.ofList)

module UnionConverterTests =
  open SharedConverterContext

  let sampleStr = "{\r\n  \"_name\": \"urn:Intelliplan.JsonNet.Tests:UnionConverterTests_Event|Created\",\r\n  \"Created\": null\r\n}"

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

  let converters = 
    [ Fac.bigIntConv :> JsonConverter
    ; Fac.unionConv :> JsonConverter
    ; Fac.tupleConv :> JsonConverter ]

  let test<'a when 'a : equality> : 'a -> unit = test converters

  let [<FactNoMono>] ``serialising simple union`` () =
    test <| D1("hello", -2)

  let [<Fact>] ``deserialising simple union`` () =
    JsonConvert.DeserializeObject(sampleStr, typeof<Event>, Fac.unionConv)
    <>? null

  let [<FactNoMono>] ``serialising union containing record`` () =
    test <| D3({ lhs = "LHS" ; rhs = "RHS" })

  let [<FactNoMono>] ``serialising union containing tuple containing record`` () =
    test <| D4({ lhs = "LHS" ; rhs = "RHS" }, -23)

  let [<FactNoMono>] ``serialising union containing tuple containing record after int`` () =
    test <| D5(-43, { lhs = "LHS" ; rhs = "RHS" })

  let [<FactNoMono>] ``serialising E2 - union containing tuple`` () =
    System.Diagnostics.Debug.Listeners.Add(new System.Diagnostics.DefaultTraceListener()) |> ignore
    test <| E2(0., "mystring", 2I, { lhs = "a"; rhs = "b" })

  let [<FactNoMono>] ``serialising nested union`` () =
    test <| D2(E1(Guid.Empty))

  let [<FactNoMono>] ``serialising complex nested union`` () =
    test <| D2(E2(-3.220000e03, "Goodbye World", 21345I, { lhs = "e"; rhs = "mail" }))
