namespace WordValues

open System

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

type WordValue = { Value : int; Warning : string option }
    with
        static member Encoder (v : WordValue) =
            Encode.object [
                ("Value", Encode.int v.Value)
                match v.Warning with
                | Some warn -> ("Warning", Encode.string warn)
                | None      -> ()
            ]

module Calculate =
    let wordValue (text : string) : WordValue =
        let (letters, nonLetters) =
            text.ToUpper()
            |> List.ofSeq
            |> List.partition (Char.IsLetter)

        let value =
            letters
            |> List.sumBy (fun letter -> (int letter) - (int 'A') + 1)

        let warning =
            if List.isEmpty nonLetters then
                None
            else
                nonLetters
                |> List.distinct
                |> List.sort
                |> List.map (sprintf "'%c'")
                |> String.concat ","
                |> sprintf "Ignored %s"
                |> Some

        {
            Value   = value
            Warning = warning
        }
