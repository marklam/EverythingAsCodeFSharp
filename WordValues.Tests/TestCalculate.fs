module WordValues.Tests.TestCalculate

open System

open Xunit
open FsCheck
open FsCheck.Xunit
open Swensen.Unquote

open Testing.Services

open WordValues

let reverse (str: string) =
    str |> Seq.rev |> Array.ofSeq |> String

// Partially bind the Testing Logger implementation
module Calculate =
    let wordValue      = Calculate.wordValue      (TestLogger.Default)
    let wordsFromValue = Calculate.wordsFromValue (TestLogger.Default)

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

let dictionary = [ ("1", 1); ("2", 2); ("5", 5); ("12", 12) ]

[<Fact>]
let ``wordsFromValue has no results for impossible totals`` () =
    test <@ Calculate.wordsFromValue dictionary  0 = [] @>
    test <@ Calculate.wordsFromValue dictionary -7 = [] @>

[<Fact>]
let ``wordsFromValue builds correct sequences`` () =
    let expectedSevens =
        [
            [ "1"; "1"; "1"; "1"; "1"; "1"; "1" ]
            [ "2"; "1"; "1"; "1"; "1"; "1" ]
            [ "2"; "2"; "1"; "1"; "1" ]
            [ "2"; "2"; "2"; "1" ]
            [ "5"; "1"; "1" ]
            [ "5"; "2" ]
        ]

    let actual =
        Calculate.wordsFromValue dictionary 7
        |> List.map (List.sortDescending)
        |> List.sort

    test <@ actual = expectedSevens @>

type WordList =
    | WordList of (string*int) list with
    static member Generator =
        let genWord =
            Arb.generate<char>
            |> Gen.filter (Char.IsLetter)
            |> Gen.arrayOf
            |> Gen.map String

        genWord
        |> Gen.map (fun w -> w, (Calculate.wordValue w).Value)
        |> Gen.filter (fun (w, v) -> v > 0)
        |> Gen.listOf
        |> Gen.map WordList
        |> Arb.fromGen

[<Property( Arbitrary=[| typeof<WordList> |] )>]
let ``wordsFromValue matches wordValue`` (WordList wordList) (PositiveInt target) =
    Calculate.wordsFromValue wordList target
    |> List.forall (fun words -> (Calculate.wordValue (String.concat " " words)).Value = target)
