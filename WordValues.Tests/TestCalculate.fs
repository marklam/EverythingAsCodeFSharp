module WordValues.Tests.TestCalculate

open System

open Xunit
open Hedgehog
open Swensen.Unquote

open Testing.Services

open WordValues

let reverse (str: string) =
    str |> Seq.rev |> Array.ofSeq |> String

// Partially bind the Testing Logger implementation
module Calculate =
    let wordValue      = Calculate.wordValue      (TestLogger.Default)
    let wordsFromValue = Calculate.wordsFromValue (TestLogger.Default)

module Gen =
    let nonNullString =
        Gen.string (Range.linear 0 100) (Gen.char Char.MinValue Char.MaxValue)

    let wordList =
        Gen.string (Range.linear 1 20) Gen.alpha
        |> Gen.map (fun w -> w, (Calculate.wordValue w).Value)
        |> Gen.list (Range.linear 0 100)

module Property =
    let regressionTest size seed prop =
        Property.recheck size seed prop
        prop

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

[<Fact>]
let ``Value of text is same as value of upper case`` () =
    property {
        let! str = Gen.nonNullString
        test <@ (Calculate.wordValue str) = Calculate.wordValue (str.ToUpper()) @>
    } |> Property.check

[<Fact>]
let ``Value of text is same as value of lower case`` () =
    property {
        let! str = Gen.nonNullString
        test <@ (Calculate.wordValue str).Value = (Calculate.wordValue (str.ToLower())).Value @>
    }
    |> Property.regressionTest 91 { Value = 9535703340393401501UL; Gamma = 8182104926013755423UL }
    |> Property.check

[<Fact>]
let ``Value of text is same as value of reversed text`` () =
    property {
        let! str = Gen.nonNullString
        test <@ Calculate.wordValue str = Calculate.wordValue (reverse str) @>
    } |> Property.check

[<Fact>]
let ``Value of text is below maximum value`` () =
    property {
        let! str = Gen.nonNullString
        test <@ (Calculate.wordValue str).Value <= 26 * str.Length @>
    }
    |> Property.regressionTest 1 { Value = 1298872065959223496UL; Gamma = 772578873708680621UL }
    |> Property.check

[<Fact>]
let ``Warning contains non-letters`` () =
    let isNonLetter c = not (( c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))

    property {
        let! str = Gen.nonNullString
        let nonLetters = str |> Seq.filter isNonLetter |> Seq.map Char.ToUpperInvariant
        let wordValue = Calculate.wordValue str

        let notWarnedAbout =
            nonLetters
            |> Seq.filter (fun c -> not (wordValue.Warning.Value.Contains(sprintf "'%c'" c)))

        test <@ Seq.isEmpty notWarnedAbout @>
    }
    |> Property.regressionTest 31 { Value = 10002960666613865206UL; Gamma = 14428377911873522553UL }
    |> Property.check

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

[<Fact>]
let ``wordsFromValue matches wordValue`` () =
    property {
        let! target = Gen.int (Range.linear 0 100)
        let! wordList = Gen.wordList
        let  result =
            Calculate.wordsFromValue wordList target
            |> List.map (fun words ->
                let str = String.concat " " words
                (str, Calculate.wordValue str))
        test <@ result |> List.forall (fun (s,v) -> v.Value = target) @>
    } |> Property.check
