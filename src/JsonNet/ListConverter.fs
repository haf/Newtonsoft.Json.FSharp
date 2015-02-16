namespace Newtonsoft.Json.FSharp

open System
open System.Reflection
open System.Collections.Generic

open Microsoft.FSharp.Reflection

open Newtonsoft.Json

/// A converter for F# lists
type ListConverter() =
  inherit JsonConverter()

  let logger = Logging.getLoggerByName "Newtonsoft.Json.FSharp.ListConverter"

  override x.CanConvert t = 
    t.IsGenericType
    && typeof<list<_>>.Equals (t.GetGenericTypeDefinition())

  override x.WriteJson(writer, value, serializer) =
    let list =
      value :?> System.Collections.IEnumerable
      |> Seq.cast

    serializer.Serialize(writer, list)

  override x.ReadJson(reader, t, _, serializer) =
    let itemType =
      t.GetGenericArguments().[0]
    let collectionType =
      typeof<IEnumerable<_>>.MakeGenericType itemType

    let collection =
      serializer.Deserialize(reader, collectionType)
      :?> System.Collections.IEnumerable
      |> Seq.cast

    let listType = typeof<list<_>>.MakeGenericType itemType

    let cases =
      FSharpType.GetUnionCases listType

    let rec make = function
      | [] ->
        FSharpValue.MakeUnion(cases.[0], [||])
      | head::tail ->
        FSharpValue.MakeUnion(cases.[1], [| head; make tail |])

    make (collection |> Seq.toList)