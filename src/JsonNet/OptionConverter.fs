namespace Newtonsoft.Json.FSharp

open System
open Microsoft.FSharp.Reflection

open Newtonsoft.Json

/// F# options-converter
type OptionConverter() =
  inherit JsonConverter()

  let logger = Logging.getLoggerByName "Newtonsoft.Json.FSharp.OptionConverter"

  override x.CanConvert t =
    t.IsGenericType
    && typeof<option<_>>.Equals (t.GetGenericTypeDefinition())

  override x.WriteJson(writer, value, serializer) =
    let value =
      if value = null then
        null
      else 
        let _,fields = FSharpValue.GetUnionFields(value, value.GetType())
        fields.[0]
    serializer.Serialize(writer, value)

  override x.ReadJson(reader, t, existingValue, serializer) =
    let innerType = t.GetGenericArguments().[0]

    let innerType = 
      if innerType.IsValueType then
        typeof<Nullable<_>>.MakeGenericType([|innerType|])
      else
        innerType

    let value = serializer.Deserialize(reader, innerType)
    let cases = FSharpType.GetUnionCases t

    if value = null then
      FSharpValue.MakeUnion(cases.[0], [||])
    else
      FSharpValue.MakeUnion(cases.[1], [|value|])