namespace Newtonsoft.Json.FSharp

open System
open System.Reflection
open System.Collections.Generic

open Microsoft.FSharp.Reflection

open Newtonsoft.Json
open Newtonsoft.Json.FSharp.Logging

/// Inspired from http://www.simontylercousins.net/journal/2013/3/21/json-converter-for-f-map-type.html
/// but doesn't do {items:[{key: k1, value: v1}]}, but {k1:v1}.
type MapConverter() =
  inherit JsonConverter()

  let logger = Logging.getLoggerByName "Newtonsoft.Json.FSharp.MapConverter"
  
  let flags = BindingFlags.Static ||| BindingFlags.NonPublic

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

    let mn   = read reader JsonToken.PropertyName |> require<string> reader
    let key  = System.Convert.ChangeType(mn, argTypes.[0])   // primitive conversion attempt
    let value = serialiser.Deserialize(reader, argTypes.[1])
    reader.Read() |> ignore // consume the value too
    FSharpValue.MakeTuple([|key; value|], FSharpType.MakeTupleType argTypes)

  override x.CanConvert t =
    t.IsGenericType
    && typeof<Map<_,_>>.Equals (t.GetGenericTypeDefinition())

  override x.WriteJson(writer : JsonWriter, o : obj, serialiser : JsonSerializer) =
    if o = null then nullArg "value"
    writeObject writer <| fun () ->
      let kvs = o :?> System.Collections.IEnumerable
      for key, value in kvs |> Seq.cast |> Seq.map (fun kv -> (key kv), (value kv)) do
        writer.WritePropertyName(key.ToString())
        serialiser.Serialize(writer, value)

  override x.ReadJson(reader, objectType, existingValue, serialiser) =
    let read, readProp, req = (read, readProp, require) $ reader
    let argTypes = objectType.GetGenericArguments()

    let tupleType =
      argTypes
      |> FSharpType.MakeTupleType

    let constructedIEnumerableType =
      typeof<IEnumerable<_>>
        .GetGenericTypeDefinition()
        .MakeGenericType(tupleType)

    let kvs = readProps reader (readKv serialiser argTypes)
    read JsonToken.EndObject |> req // we need to consume the object close

    let kvsn = System.Array.CreateInstance(tupleType, kvs.Length)
    System.Array.Copy(kvs, kvsn, kvs.Length)

    Logger.debug logger <| fun _ ->
      LogLine.sprintf
        [ "path",       reader.Path |> box
          "token_type", reader.TokenType |> box
          "values",     box kvsn ]
        "creating map from keys and values"

    let methodInfo = objectType.GetMethod("Create", flags, null, [|constructedIEnumerableType|], null)
    methodInfo.Invoke(null, [|kvsn|])
//    debug <| sprintf "Source array: %s" (kvs.GetType().ToString())
//    debug <| sprintf "Target array: %s" (kvsn.GetType().ToString())
//    debug <| sprintf "IEnumerable: %s" (constructedIEnumerableType.ToString())
