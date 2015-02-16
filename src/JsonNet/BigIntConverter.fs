namespace Newtonsoft.Json.FSharp

open System
open System.Reflection
open Microsoft.FSharp.Reflection

open Newtonsoft.Json
open Newtonsoft.Json.FSharp.TypeNaming

/// Converter for BigInteger types from F#.
type BigIntConverter() =
  inherit JsonConverter()

  let logger = Logging.getLoggerByName "Newtonsoft.Json.FSharp.BigIntConverter"

  override x.CanConvert t =
    typeof<Microsoft.FSharp.Core.bigint>.Equals t

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
