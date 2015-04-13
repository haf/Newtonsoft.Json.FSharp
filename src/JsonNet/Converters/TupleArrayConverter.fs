namespace Newtonsoft.Json.FSharp

open System
open Microsoft.FSharp.Reflection

open Newtonsoft.Json
open Newtonsoft.Json.FSharp.Logging

/// Converter converting tuples to arrays
type TupleArrayConverter() =
  inherit JsonConverter()

  let logger = Logging.getLoggerByName "Newtonsoft.Json.FSharp.TupleArrayConverter"

  override x.CanConvert t =
    FSharpType.IsTuple t

  override x.WriteJson(writer, value, serialiser) =
    let values = FSharpValue.GetTupleFields(value)
    serialiser.Serialize(writer, values)

  override x.ReadJson(reader, t, _, serialiser) =
    let read, readProp, req = (read, readProp, require) $ reader

    Logger.debug logger (fun _ -> LogLine.sprintf ["type", box t] "reading json")

    let itemTypes = FSharpType.GetTupleElements t

    let readElements () =
      let rec iter index acc =
        match reader.TokenType with
        | JsonToken.EndArray ->
          acc

        | JsonToken.StartObject ->
          Logger.debug logger <| fun _ ->
            LogLine.sprintf
              [ "path",       reader.Path |> box
                "token_type", reader.TokenType |> box ]
              "pre-deserialise"

          let value = serialiser.Deserialize (reader, itemTypes.[index])
          read JsonToken.EndObject |> req |> ignore

          Logger.debug logger <| fun _ ->
            LogLine.sprintf
              [ "path", reader.Path |> box
                "token_type", reader.TokenType |> box ]
              "post-deserialise"

          iter (index + 1) (acc @ [value])

        | JsonToken.Boolean
        | JsonToken.Bytes
        | JsonToken.Date
        | JsonToken.Float
        | JsonToken.Integer
        | JsonToken.String ->

          Logger.debug logger <| fun _ ->
            LogLine.sprintf
              [ "path", reader.Path |> box
                "token_type", reader.TokenType |> box ]
              "value token, pre-deserialise"

          let value = serialiser.Deserialize(reader, itemTypes.[index])

          Logger.debug logger <| fun _ ->
            LogLine.sprintf
              [ "path", reader.Path |> box
                "token_type", reader.TokenType |> box ]
              "value token, post-deserialise"

          reader.Read () |> ignore
          iter (index + 1) (acc @ [value])

        | JsonToken.Null ->
          failwithf "While JS tolerates nulls, F# services don't - null found at '%s'"
                    reader.Path

        | _ as token ->
          failwithf "TupleArray: Unknown intermediate token '%A' at path '%s'"
                    token
                    reader.Path

      reader.Read () |> ignore
      iter 0 List.empty

    match reader.TokenType with
    | JsonToken.StartArray ->
      let values = readElements()
      let res = FSharpValue.MakeTuple (values |> List.toArray, t)
      read JsonToken.EndArray |> req |> ignore
      res

    | _ ->
      failwithf "TupleArray: invalid END token '%A' at path: '%s'"
                reader.TokenType
                reader.Path