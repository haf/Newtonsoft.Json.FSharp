/// Module interface for the goodies in the Newtonsoft.Json.FSharp assembly.
module Newtonsoft.Json.FSharp.Serialisation

open System.IO

open Newtonsoft.Json
open Newtonsoft.Json.FSharp

/// Extend the passed JsonSerializerSettings with
/// the converters defined in this module/assembly.
[<CompiledName "ConfigureForFSharp">]
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
let serialise opts o =
  let name = TypeNaming.nameObj o
  use ms = new MemoryStream()
  (use jsonWriter = new JsonTextWriter(new StreamWriter(ms))
   let s = JsonSerializer.Create opts
   s.Serialize(jsonWriter, o))
  name, ms.ToArray()

let serialiseNoOpts o =
  serialise opts o

/// Deserialise to the type t, from the data in the byte array
let deserialise opts (t, data:byte array) =
  use ms = new MemoryStream(data)
  use jsonReader = new JsonTextReader(new StreamReader(ms))
  let s = JsonSerializer.Create opts
  s.Deserialize(jsonReader, t)

let deserialiseNoOpts o =
  deserialise opts o

/// Return the serialize and deserialize methods.
/// The serialize method takes an object and returns its event
/// type as a string and a byte array with the serialized data
let serialiser opts = serialise opts, deserialise opts

/// Shortcut with non-configurable JsonOptions
let serialiserPair = serialiseNoOpts, deserialiseNoOpts