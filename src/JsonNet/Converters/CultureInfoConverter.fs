namespace Newtonsoft.Json.FSharp

open System
open System.Globalization
open Newtonsoft.Json

/// Reads and writes <see cref="System.Globlization.CultureInfo" />.
type CultureInfoConverter() =
  inherit JsonConverter()

  override x.CanConvert(typ:Type) =
    typ = typeof<CultureInfo>

  override x.WriteJson(writer: JsonWriter, value: obj, serializer: JsonSerializer) =
    match value with
    | null                   -> writer.WriteNull()
    | :? CultureInfo as info -> info.Name |> writer.WriteValue
    | _                      -> raise (JsonWriterException(sprintf "unknown value for CultureInfoConverter: %A" value))

  override x.ReadJson(reader: JsonReader, objectType: Type, existingValue: obj, serializer: JsonSerializer) =
    match reader.TokenType with
    | JsonToken.Null -> null
    | JsonToken.String ->
      let s = reader.Value :?> string
      box (CultureInfo s)
    | _ as v -> raise (JsonReaderException(sprintf "unhandled case %s" (v.ToString("G"))))