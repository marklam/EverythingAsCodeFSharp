module WordValues.Tests.TestCalculate

open System

open Xunit
open FsCheck
open FsCheck.Xunit
open Swensen.Unquote

open WordValues

let reverse (str: string) =
    str |> Seq.rev |> Array.ofSeq |> String

[<Fact>]
let ``Value of 'HELLO' is correct`` () =
      test <@ (Calculate.wordValue "HELLO").Value = 8 + 5 + 12 + 12 + 15 @>

[<Fact>]
let ``Value of 'hello' is correct`` () =
      test <@ (Calculate.wordValue "hello").Value = 8 + 5 + 12 + 12 + 15 @>

[<Fact>]
let ``No warnings produced for 'hello'`` () =
      test <@ (Calculate.wordValue "hello").Warning = None @>

[<Fact>]
let ``Value of 'HELLO 123' contains warnings`` () =
      test <@ (Calculate.wordValue "HELLO 123").Warning = Some "Ignored ' ','1','2','3'" @>

[<Property>]
let ``Value of text is same as value of upper case`` (nnstr : NonNull<string>) =
    let str = nnstr.Get
    (Calculate.wordValue str) = Calculate.wordValue (str.ToUpper())

[<Property>]
let ``Value of text is same as value of lower case`` (nnstr : NonNull<string>) =
    let str = nnstr.Get
    Calculate.wordValue str = Calculate.wordValue (str.ToLower())

[<Property>]
let ``Value of text is same as value of reversed text`` (nnstr : NonNull<string>) =
    let str = nnstr.Get
    Calculate.wordValue str = Calculate.wordValue (reverse str)

[<Property>]
let ``Value of text is below maximum value`` (nnstr : NonNull<string>) =
    let str = nnstr.Get
    (Calculate.wordValue str).Value <= 26 * str.Length

[<Property>]
let ``Warning contains non-letters`` (nnstr : NonNull<string>) =
    let str = nnstr.Get
    let nonLetters = str |> Seq.filter (not << Char.IsLetter)
    let wordValue = Calculate.wordValue str

    let notWarnedAbout =
        nonLetters
        |> Seq.filter (fun c -> not (wordValue.Warning.Value.Contains(sprintf "'%c'" c)))

    Seq.isEmpty notWarnedAbout

