namespace Newtonsoft.Json.FSharp

open System
open Newtonsoft.Json

/// Reads and writes <see cref="System.Uri" />.
type UriConverter() =
  inherit JsonConverter()

  override x.CanConvert(typ:Type) =
    typ = typeof<Uri>

  override x.WriteJson(writer: JsonWriter, value: obj, serializer: JsonSerializer) =
    match value with
    | null        -> writer.WriteNull()
    | :? Uri as u -> u.OriginalString |> writer.WriteValue
    | _           -> raise <| JsonWriterException( sprintf "unknown value for UriConverter: %A" value )

  override x.ReadJson(reader: JsonReader, objectType: Type, existingValue: obj, serializer: JsonSerializer) =
    match reader.TokenType with
    | JsonToken.Null   -> null
    | JsonToken.String ->
      let s = reader.Value :?> string
      match Uri.TryCreate(s, UriKind.RelativeOrAbsolute) with
      | false, _ -> raise <| JsonReaderException( sprintf "the uri %s doesn't parse" s )
      | true, v  -> box v
    | _ as v -> raise <| JsonReaderException( sprintf "unhandled case %s" (v.ToString("G")) )
