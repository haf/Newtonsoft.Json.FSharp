# Newtonsoft.Json.FSharp

Nice F# support for Newtonsoft.JSON - tuples as arrays, maps as objects, lists as arrays, unions as _name-metadata annotated arrays, decimals as strings to save precision, options as null/value, string-GUIDs and finally BigInt support.

Sponsored by
[qvitoo – A.I. bookkeeping(https://qvitoo.com/?utm_source=github&utm_campaign=repos).

## Usage

You have three choices;

### 1. You can instantiate the converter:

```
open Newtonsoft.Json.FSharp
let str = JsonConvert.Serialize(o, [| GuidConverter() :> JsonConverter |])
```

### 2. You can add the converters:

```
open Newtonsoft.Json.FSharp
let str = JsonConvert.Serialize(o, Serialisation.converters)
```

### 3. You can extend `JsonSerializerSettings` with it

This is a nice way to do it, because you can then pass those settings into your Bootstrapper/Composition Root of your program. Remember you need to acutally *use* the settings.

```
open Newtonsoft.Json.FSharp
let opts = Serialisation.extend (JsonSerializerSettings()) // this goes global
let str = JsonConvert.Serialize(o, opts) // this is local
```

## The Converters

You can find [all the converters in the source tree](https://github.com/haf/Newtonsoft.Json.FSharp/tree/master/src/JsonNet/Converters).

The convert both from and to.

### BigInt Converter

```
267321267876321I
=> {
  "_name": "System.Numeric.BigInt, System.Numeric"
  "value": "267321267876321"
}
```

### CultureInfo Converter

```
CultureInfo "sv-SE"
=> "sv-SE"
```

### Guid Converter

```
Guid.NewGuid()
=> "6ac5c744-ad0b-412c-bf11-9fb732344d90"
```

### List Converter

```
[ 2;3;4;5 ]
=> [ 2,3,4,5 ]
```

### Map Converter

[ "hello", "world"; "you", "there" ]
=> { "hello": "world", "you": "there" }

### Option Converter

```
type A = { a : string; b : int option }
{ a = "hi"; b = None }
=> { "a": "hi", "b": null }

{ a = "hi"; b = Some 5 }
=>  { "a": "hi", "b": 5 }
```

### Tuple Array Converter

```
"3", 4e11
=> [ "3", 4e11 ]
```

### Union Converter

```
type Drinks =
  | Swimmingpool
  | IrishCoffee of teaSpoonsSugar:int
Swimmingpool
=> {
  "_name": "Drinks|Swimmingpool",
  "value": null
}

IrishCoffee 3
=> {
  "_name": "Drinks|IrishCoffee",
  "value": [ 3 ]
}
```

### Uri Converter

```
Uri "http://haf.se"
=> "http://haf.se"
```
