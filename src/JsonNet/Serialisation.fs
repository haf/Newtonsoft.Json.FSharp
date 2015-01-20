namespace Intelliplan.JsonNet

/// Module containing Newtonsoft.Json converters
open TypeNaming
open Newtonsoft.Json
open Newtonsoft.Json.Converters
open Newtonsoft.Json.Serialization
open System
open System.Reflection
open System.Collections.Generic
open Microsoft.FSharp.Reflection

[<AutoOpen>]
module internal SerialisationFunctions =

//  let debug = System.Diagnostics.Debug.WriteLine

  // parser functions

  let fail (reader : JsonReader) token =
    let msg = sprintf "Invalid token '%A', at path '%s'" token reader.Path
    raise <| JsonReaderException(msg)

  let read (reader : JsonReader) (t : JsonToken) =
    if reader.TokenType = t then
      let value = reader.Value
      reader.Read() |> ignore
      Some value
    else None

  let require<'a> reader v =
    match v with
    | Some o -> (o : obj) :?> 'a
    | None   -> fail reader v

  let readProp (reader : JsonReader) (n : string) =
    read reader JsonToken.PropertyName |> Option.map (fun v -> if (v :?> string) <> n then fail reader n else v)

  let ($) ts arg =
    match (ts, arg) with
    | (f1, f2, f3), arg -> f1 arg, f2 arg, f3 arg

  // writer functions

  let writeObject (writer : JsonWriter) f =
    writer.WriteStartObject()
    f()
    writer.WriteEndObject()

/// Converter for BigInteger types from F#.
type BigIntConverter() =
  inherit JsonConverter()
  let typ = typeof<Microsoft.FSharp.Core.bigint>

  override x.CanConvert t =
    typ.Equals t

  override x.WriteJson(writer, value, serializer) =
    let typ = value.GetType()
    let bi = value :?> bigint
    writer.WriteStartObject()
    writer.WritePropertyName("_name")
    writer.WriteValue(TypeNaming.name value typ)
    writer.WritePropertyName("value")
    writer.WriteValue(bi.ToString())
    writer.WriteEndObject()

  override x.ReadJson(reader, t, _, serializer) =
    let read, readProp, req = (read, readProp, require) $ reader
    read JsonToken.StartObject |> req |> ignore
    readProp "_name" |> req |> ignore      // _name property
    read JsonToken.String |> req |> ignore // _name value
    readProp "value" |> req |> ignore
    match read JsonToken.String |> require<string> reader |> bigint.TryParse with
    | true, value -> box value
    | false, _    -> box 0I

/// GUID converter
type GuidConverter() =
  inherit JsonConverter()

  override x.CanConvert(t:Type) = t = typeof<Guid>

  override x.WriteJson(writer, value, serializer) =
    let value = value :?> Guid
    if value <> Guid.Empty then writer.WriteValue(value.ToString("N"))
    else writer.WriteValue("")
        
  override x.ReadJson(reader, t, _, serializer) =
    match reader.TokenType with
    | JsonToken.Null -> Guid.Empty :> obj
    | JsonToken.String ->
      let str = reader.Value :?> string
      if (String.IsNullOrEmpty(str)) then Guid.Empty :> obj
      else new Guid(str) :> obj
    | _ -> failwith "Invalid token when attempting to read Guid."

/// A converter for F# lists
type ListConverter() =
  inherit JsonConverter()

  override x.CanConvert(t:Type) = 
    t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<list<_>>

  override x.WriteJson(writer, value, serializer) =
    let list = value :?> System.Collections.IEnumerable |> Seq.cast
    serializer.Serialize(writer, list)

  override x.ReadJson(reader, t, _, serializer) =
    let itemType = t.GetGenericArguments().[0]
    let collectionType = typedefof<IEnumerable<_>>.MakeGenericType(itemType)
    let collection = serializer.Deserialize(reader, collectionType) :?> System.Collections.IEnumerable |> Seq.cast
    let listType = typedefof<list<_>>.MakeGenericType(itemType)
    let cases = FSharpType.GetUnionCases(listType)
    let rec make = function
      | [] -> FSharpValue.MakeUnion(cases.[0], [||])
      | head::tail -> FSharpValue.MakeUnion(cases.[1], [| head; (make tail); |])
    make (collection |> Seq.toList)

/// F# options-converter
type OptionConverter() =
  inherit JsonConverter()

  override x.CanConvert(t) =
    t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<option<_>>

  override x.WriteJson(writer, value, serializer) =
    let value =
      if value = null then null
      else 
        let _,fields = FSharpValue.GetUnionFields(value, value.GetType())
        fields.[0]
    serializer.Serialize(writer, value)

  override x.ReadJson(reader, t, existingValue, serializer) =
    let innerType = t.GetGenericArguments().[0]
    let innerType = 
      if innerType.IsValueType then typedefof<Nullable<_>>.MakeGenericType([|innerType|])
      else innerType
    let value = serializer.Deserialize(reader, innerType)
    let cases = FSharpType.GetUnionCases(t)
    if value = null then FSharpValue.MakeUnion(cases.[0], [||])
    else FSharpValue.MakeUnion(cases.[1], [|value|])

type internal JT = JsonToken

/// Converter converting tuples to arrays
type TupleArrayConverter() =
  inherit JsonConverter()

  override x.CanConvert(t:Type) =
    FSharpType.IsTuple(t)

  override x.WriteJson(writer, value, serializer) =
    let values = FSharpValue.GetTupleFields(value)
    serializer.Serialize(writer, values)

  override x.ReadJson(reader, t, _, serializer) =
    let read, readProp, req = (read, readProp, require) $ reader
//    debug <| sprintf "TupleArray json for %s" (t.ToString())

    let advance = reader.Read >> ignore
    let deserialize t = serializer.Deserialize(reader, t)
    let itemTypes = FSharpType.GetTupleElements(t)

    let readElements() =
      let rec iter index acc =
        match reader.TokenType with
        | JT.EndArray -> acc
        | JT.StartObject ->
//          debug <| sprintf "obj token@%s [pre-deserialise] %A" reader.Path reader.TokenType
          let value = deserialize itemTypes.[index]
          read JT.EndObject |> req |> ignore
//          debug <| sprintf "obj token@%s [post-deserialise] %A" reader.Path reader.TokenType
          iter (index + 1) (acc @ [value])
        | JT.Boolean | JT.Bytes | JT.Date | JT.Float | JT.Integer | JT.String ->
//          debug <| sprintf "val token@%s [pre-deserialise] %A" reader.Path reader.TokenType
          let value = deserialize(itemTypes.[index])
//          debug <| sprintf "val token@%s [post-deserialise] %A" reader.Path reader.TokenType
          advance ()
          iter (index + 1) (acc @ [value])
        | JT.Null -> failwith <| sprintf "While JS tolerates nulls, F# services don't - null found at '%s'" reader.Path
        | _ as t -> failwith <| sprintf "Unknown token '%A' at path '%s'" t reader.Path
      advance ()
      iter 0 List.empty

    match reader.TokenType with
    | JsonToken.StartArray ->
      let values = readElements()
      let res = FSharpValue.MakeTuple(values |> List.toArray, t)
      read JsonToken.EndArray |> req |> ignore
      res
    | _ -> failwith <| sprintf "invalid token type: %A at path: '%s'" reader.TokenType reader.Path

// {
//   "_name": "urn:Intelliplan.JsonNet.Tests:A|D1",
//  "D1": ["hello",-2]
// }
// {
//  "_name": "urn:Intelliplan.JsonNet.Tests:A|D2",
//  "D2": {
//    "_name": "urn:Intelliplan.JsonNet.Tests:B|E1",
//    "E1": "00000000-0000-0000-0000-000000000000"
//  }
// }
/// Discriminated union converter
type UnionConverter() =
  inherit JsonConverter()

  override x.CanConvert typ =
    FSharpType.IsUnion(typ) || (typ.DeclaringType <> null && FSharpType.IsUnion(typ.DeclaringType))

  override x.WriteJson(writer, value, serializer) =
    let t = value.GetType()
    let caseInfo, fieldValues = FSharpValue.GetUnionFields(value, t)
    writer.WriteStartObject()
    writer.WritePropertyName("_name")
    writer.WriteValue(TypeNaming.name value t)
    writer.WritePropertyName(caseInfo.Name)
    let value =
        match fieldValues.Length with
        | 0 -> null
        | 1 -> fieldValues.[0]
        | _ -> fieldValues :> obj
    serializer.Serialize(writer, value)
    writer.WriteEndObject()

  override x.ReadJson(reader, t, _, serializer) =
    let t = if FSharpType.IsUnion(t) then t else t.DeclaringType
    let read, readProp, req = (read, readProp, require) $ reader
//    debug <| sprintf "Union json for %s" (t.ToString())

    read JsonToken.StartObject |> req |> ignore
    readProp "_name" |> ignore
    let urnType = read JsonToken.String |> require<string> reader (* e.g. urn:Intelliplan.JsonNet.Tests:A|D1 *)
    let typeData = TypeNaming.parse urnType
    let caseName = typeData.CaseName |> Option.map box |> require<string> reader
    readProp caseName |> req |> ignore

    let caseInfo = FSharpType.GetUnionCases(t) |> Seq.find (fun c -> c.Name = caseName)
    let fields = caseInfo.GetFields()
    let toUnion args = FSharpValue.MakeUnion(caseInfo, args)

    match fields.Length with
    | 0 ->
      read JsonToken.Null |> req |> ignore // null because there are no args
      FSharpValue.MakeUnion(caseInfo, [||])
    | 1 -> 
      let res = [| serializer.Deserialize(reader, fields.[0].PropertyType) |]
      reader.Read() |> ignore // at end of some object
      res |> toUnion
    | _ ->
      let tupleType = FSharpType.MakeTupleType(fields |> Seq.map (fun f -> f.PropertyType) |> Seq.toArray)
      let tuple = serializer.Deserialize(reader, tupleType) // array consumes end array token
      FSharpValue.GetTupleFields(tuple) |> toUnion

open System.Linq

/// Inspired from http://www.simontylercousins.net/journal/2013/3/21/json-converter-for-f-map-type.html
/// but doesn't do {items:[{key: k1, value: v1}]}, but {k1:v1}.
type MapConverter() =
  inherit JsonConverter()

  let flags = BindingFlags.Static ||| BindingFlags.NonPublic

  // TODO: optimise reflection (see above)
  let key (kvp : obj) = kvp.GetType().GetProperty("Key").GetValue(kvp, null)
  let value (kvp : obj) = kvp.GetType().GetProperty("Value").GetValue(kvp, null)

  let readProps (reader : JsonReader) readFun =
    let read, require = read reader, require reader
    read JsonToken.StartObject |> require // start of JSON map object
    if reader.TokenType = JsonToken.PropertyName then
      [|
        while reader.TokenType <> JsonToken.EndObject do
          let element = readFun reader
          yield element
      |]
    else
      Array.empty

  let readKv (serialiser : JsonSerializer) (argTypes : Type array) (reader : JsonReader) =
    if reader.TokenType <> JsonToken.PropertyName then
      failwith "expected property name here"
    else
      let mn   = read reader JsonToken.PropertyName |> require<string> reader
      let key  = System.Convert.ChangeType(mn, argTypes.[0])   // primitive conversion attempt
      let value = serialiser.Deserialize(reader, argTypes.[1])
      reader.Read() |> ignore // consume the value too
      FSharpValue.MakeTuple([|key;value|], FSharpType.MakeTupleType(argTypes))

  override x.CanConvert(typ:Type) =
    typ.IsGenericType && typ.GetGenericTypeDefinition() = typedefof<Map<_,_>>

  override x.WriteJson(writer : JsonWriter, o : obj, serialiser : JsonSerializer) =
    if o = null then nullArg "value"
    writeObject writer <| fun () ->
      let kvs = o :?> System.Collections.IEnumerable
      for key, value in kvs |> Seq.cast |> Seq.map (fun kv -> (key kv), (value kv)) do
        writer.WritePropertyName(key.ToString())
        serialiser.Serialize(writer, value)

  override x.ReadJson(reader : JsonReader, objectType : Type, existingValue : obj, serialiser : JsonSerializer) =
    let read, readProp, req = (read, readProp, require) $ reader
    let argTypes = objectType.GetGenericArguments()
    let tupleType = argTypes |> FSharpType.MakeTupleType
    let constructedIEnumerableType = typedefof<IEnumerable<_>>.GetGenericTypeDefinition().MakeGenericType(tupleType)

    let kvs = readProps reader (readKv serialiser argTypes)
    read JsonToken.EndObject |> req // we need to consume the object close

    let kvsn = System.Array.CreateInstance(tupleType, kvs.Length)
    System.Array.Copy(kvs, kvsn, kvs.Length)
    let methodInfo = objectType.GetMethod("Create", flags, null, [|constructedIEnumerableType|], null)
    methodInfo.Invoke(null, [|kvsn|])
//    debug <| sprintf "Source array: %s" (kvs.GetType().ToString())
//    debug <| sprintf "Target array: %s" (kvsn.GetType().ToString())
//    debug <| sprintf "IEnumerable: %s" (constructedIEnumerableType.ToString())

open System.Runtime.CompilerServices

/// Module interface for the goodies in the Intelliplan.JsonNet assembly.
[<Extension>]
module Serialisation =

  /// Extend the passed JsonSerializerSettings with
  /// the converters defined in this module/assembly.
  [<CompiledName("ConfigureForFSharp"); Extension>]
  let extend (s : JsonSerializerSettings) =
    s.Converters.Add <| new GuidConverter()
    s.Converters.Add <| new TupleArrayConverter()
    s.Converters.Add <| new OptionConverter()
    s.Converters.Add <| new ListConverter()
    s.Converters.Add <| new UnionConverter()
    s.Converters.Add <| new MapConverter()
    s.NullValueHandling <- NullValueHandling.Ignore
    s

  let private opts = JsonSerializerSettings() |> extend
  let private s = JsonSerializer.Create opts

  /// Serialise the passed object to JSON using the default
  /// JsonSerializer settings, and return the type name
  /// and the data of the type as a tuple. Uses indented formatting.
  [<CompiledName("Serialise"); Extension>]
  let serialise opts o =
    let name = TypeNaming.nameObj o
    use ms = new IO.MemoryStream()
    use jsonWriter = new JsonTextWriter(new IO.StreamWriter(ms))
    let s = JsonSerializer.Create opts
    s.Serialize(jsonWriter, o)
    name, ms.ToArray()

  let serialise' o =
    serialise opts o

  /// Deserialise to the type t, from the data in the byte array
  let deserialise opts (t, data:byte array) =
    use ms = new IO.MemoryStream(data)
    use jsonReader = new JsonTextReader(new IO.StreamReader(ms))
    let s = JsonSerializer.Create opts
    s.Deserialize(jsonReader, t)

  let deserialise' o =
    deserialise opts o

  /// Return the serialize and deserialize methods.
  /// The serialize method takes an object and returns its event
  /// type as a string and a byte array with the serialized data
  let serialiser opts = serialise opts, deserialise opts

  /// Shortcut with non-configurable JsonOptions
  let serialiser' = serialise, deserialise