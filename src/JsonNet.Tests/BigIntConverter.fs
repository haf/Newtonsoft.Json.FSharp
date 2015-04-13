module Newtonsoft.Json.FSharp.Tests.BigIntConverter

open System
open System.Numerics

open Fuchu
open Swensen.Unquote
open Newtonsoft.Json
open Newtonsoft.Json.FSharp

// https://bugzilla.xamarin.com/show_bug.cgi?id=12233

// using the big int converter
let test = test' (BigIntConverter())

[<Tests>]
let tests =
  testList "can serialise BigInt" [
    testCase "serialising simple bigint" <| fun _ -> test 345I
    testCase "serialising negative bitint" <| fun _ -> test -234I
    testCase "serialising large bigint" <| fun _ -> test -1298398939838934983893893893893893893838389I
    //testProp "serialising large bigint" <| fun (i : BigInteger) -> test i
  ]