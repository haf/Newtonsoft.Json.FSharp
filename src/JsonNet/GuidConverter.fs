namespace Newtonsoft.Json.FSharp

open System

open Newtonsoft.Json

/// GUID converter
type GuidConverter() =
  inherit JsonConverter()

  let logger = Logging.getLoggerByName "Newtonsoft.Json.FSharp.GuidConverter"

  override x.CanConvert t =
    typeof<Guid>.Equals t

  override x.WriteJson(writer, value, serializer) =
    let value = value :?> Guid
    if value <> Guid.Empty then
      writer.WriteValue(value.ToString("N"))
    else
      writer.WriteValue("")

  override x.ReadJson(reader, t, _, serializer) =
    match reader.TokenType with
    | JsonToken.Null ->
      Guid.Empty :> obj

    | JsonToken.String ->
      match reader.Value :?> string with
      | str when String.IsNullOrEmpty str ->
        Guid.Empty |> box
      | str ->
        new Guid(str) |> box

    | _ -> failwith "Invalid token when attempting to read Guid."
