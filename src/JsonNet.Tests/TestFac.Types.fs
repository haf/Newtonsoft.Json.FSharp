namespace Newtonsoft.Json.FSharp.Tests

type Outer = | D | E
type Outer2 = | F | G
and Outer2Inner = | H

module Principal =

  type Cmd =
    | X