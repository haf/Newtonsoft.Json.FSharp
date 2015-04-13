[<AutoOpen>]
module internal Newtonsoft.Json.FSharp.Prelude

open Newtonsoft.Json

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
