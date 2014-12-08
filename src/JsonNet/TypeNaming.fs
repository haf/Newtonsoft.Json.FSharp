namespace Intelliplan.JsonNet

// ok, so this is ugly, but unit tested and working
module TypeNaming =

  open System
  open System.IO
  open System.Text

  type UrnTypeName =
    { Namespace : string
    ; Name      : string
    ; CaseName  : string option }
    override x.ToString() = sprintf "{ Namespace=%s; Name=%s; CaseName=%A }" x.Namespace x.Name x.CaseName
    static member Empty = { Namespace = ""; Name = ""; CaseName = None }

  open Microsoft.FSharp.Reflection
  type BF=System.Reflection.BindingFlags

  /// Converts a string into a list of characters.
  let internal explode (s:string) =
    [for c in s -> c]

  /// Converts a list of characters into a string.
  let internal implode (xs:char list) =
    let sb = System.Text.StringBuilder(xs.Length)
    xs |> List.iter (sb.Append >> ignore)
    sb.ToString()

  let internal (|Pre|_|) (p:string) (s:string) =
    if s.StartsWith(p) then
        Some(s.Substring(p.Length))
    else
        None

  /// Return None if c-char is never met
  let internal (|Until|_|) (c : char) (s : string) =
    let chomped = explode s |> Seq.takeWhile (fun c' -> not (c' = c)) |> Seq.toList |> implode
    if chomped.Length = 0 then None
    elif chomped.Length = s.Length then None
    else Some(chomped, s.Substring(chomped.Length))

  type internal Concern =
    { readableName : string
    ; applies      : Type -> bool
    ; apply        : Type -> obj -> string * Type option
    ; deconstruct  : string * UrnTypeName -> string * UrnTypeName option }

  let private nameConcerns : Concern list =
    [
      { readableName = "add urn at start"
      ; applies      = fun typ -> true
      ; apply        = fun typ v -> "urn:", Some(typ)
      ; deconstruct  = function
      // remove urn: prefix
          | Pre "urn:" rest, state -> rest, Some(UrnTypeName.Empty)
          | s, state               -> s, None }

    ; { readableName = "add namespace"
      ; applies      = fun typ -> true
      ; apply        = fun typ v -> (sprintf "%s:" typ.Namespace), Some(typ)
      ; deconstruct  = function
      // parse namespace
          | Until ':' (ns, rest), state -> rest.TrimStart(':'), Some({ state with Namespace = ns })
          | _                           -> "", None }

    ; { readableName = "ordinary types"
      ; applies      = fun typ   -> typ.DeclaringType = null && not <| FSharpType.IsUnion(typ)
      ; apply        = fun typ v -> (sprintf "%s" typ.Name), Some(typ)
      ; deconstruct  = fun (str, state) -> str, Some({ state with Name = str }) }

    ; { readableName = "for nested types"
      ; applies      = fun typ -> typ.DeclaringType <> null && not <| FSharpType.IsUnion(typ.DeclaringType)
      ; apply        = fun typ v ->
        let start = typ.DeclaringType.FullName.LastIndexOf '.'
        let full_parent = typ.DeclaringType.FullName.Substring (start + 1)
        (sprintf "%s_" <| full_parent.Replace("+", "_")), Some(typ)
      // not used
      ; deconstruct  = (fun _ -> "", None) }

    ; { readableName = "disc. type owned discriminated union names"
      ; applies      = fun typ -> typ.DeclaringType <> null && FSharpType.IsUnion(typ.DeclaringType)
      ; apply        = fun typ v -> (sprintf "%s|%s" typ.DeclaringType.Name typ.Name), None
      ; deconstruct  = function
      // for getting type name
          | Until '|' (tname, rest), state -> rest, Some({ state with Name = tname })
          | _                              -> "", None }

    ; { readableName = "module-owned disc. unions"
      ; applies      = fun typ -> typ.DeclaringType <> null && FSharpType.IsUnion(typ)
      ; apply        = fun typ v ->
          let caseInfo, values = FSharpValue.GetUnionFields(v, typ, BF.Public ||| BF.NonPublic)
          (sprintf "%s|%s" typ.Name caseInfo.Name ), None
      ; deconstruct  = function
      // for getting case name
          | Pre "|" rest, state -> rest, Some({ state with CaseName = Some(rest) })
          | _                   -> "", None }

    ; { readableName = "free-standing union types"
      ; applies      = fun typ -> typ.DeclaringType = null && FSharpType.IsUnion(typ)
      ; apply        = fun typ v ->
          let caseInfo, values = FSharpValue.GetUnionFields(v, typ, BF.Public ||| BF.NonPublic)
          (sprintf "%s|%s" typ.Name caseInfo.Name ), None
      // not used
      ; deconstruct  = (fun _ -> "", None) }
    ]
  
  let name v (t : System.Type) =
    let concerns = nameConcerns |> List.filter (fun { applies = q; } -> q t)
    /// apply the concerns passed to the type t, accumulating the built name
    /// in the string builder
    let rec apply concerns t (acc : StringBuilder) =
      match concerns with
      | [] -> acc.ToString()
      | { readableName = n
        ; apply        = fa } :: restConcerns ->
        match fa t v with
        | _ as partName, Some(t') ->
          //System.Diagnostics.Debug.WriteLine(sprintf "looking at: %s, adding: %s, next type: %s" n partName t'.FullName)
          apply restConcerns t' (acc.Append(partName))
        | _ as partName, None     -> acc.Append(partName).ToString()

    apply concerns t (new StringBuilder())

  /// Get the name for an object instance.
  let nameObj (v : 'a) = name v (v.GetType())

  /// parse a URN to a UrnTypeName
  let parse urn =
    let rec parse' = function
      | [], rest, nameRec                        -> nameRec
      | { readableName = n
        ; deconstruct = f } :: cs, rest, nameRec ->
        match f( rest, nameRec ) with
        // stop when None is returned
        | rest', None           -> parse'(cs, rest, nameRec)
        // recurse, decreasing the look variant; the number of concerns
        | rest', Some(nameRec') ->
          //System.Diagnostics.Debug.WriteLine(sprintf "[%s] matches, current state: %s" n (nameRec.ToString()))
          parse'(cs, rest', nameRec')
    // start recursion
    parse'(nameConcerns, urn, UrnTypeName.Empty)

  type Type with
    /// Convert the type to a partially qualified name that also contains
    /// the types of the type parameters (if the type is generic).
    /// Throws argument exception if the type is an open generic type.
    member t.ToPartiallyQualifiedName () =
      if t.IsGenericTypeDefinition then invalidArg "open generic types are not allwed" "t"
      let sb = new StringBuilder()
      let append = (fun s -> sb.Append(s) |> ignore) : string -> unit

      append t.FullName

      if t.IsGenericType then
        append "["
        let args = t.GetGenericArguments() |> Array.map (fun g -> sprintf "[%s]" <| g.ToPartiallyQualifiedName())
        append <| String.Join(", ", args)
        append "]"
    
      append ", "
      append <| t.Assembly.GetName().Name
    
      sb.ToString()
