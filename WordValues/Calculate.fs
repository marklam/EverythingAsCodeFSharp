namespace WordValues

open System

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

open Services

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
    let wordValue (logger : ILogger) (text : string) : WordValue =
        Log.info logger (LogEvent.Create("wordValue of {text}", text))

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

        Log.info logger (LogEvent.Create("wordValue returning value {value} with warning {warning}", value, warning))

        {
            Value   = value
            Warning = warning
        }

    let wordsFromValue (logger : ILogger) (wordValues : (string*int) list) (value : int) : string list list =
        let rec fit acc ws t =
            match ws, t with
            | _          , 0 -> [acc] // That's a solution
            | []         , _ -> []    // No more words to try
            | (w,v)::rest, _ ->
                (if (t < v) then [] else fit (w::acc) ws (t - v)) // Use w and fit the remainder
                @ (fit acc rest t)                                // Also try without using w

        Log.info logger (LogEvent.Create("wordsFromValue seeking {value}", value))

        let result =
            fit [] wordValues value
            |> List.filter (not << List.isEmpty)

        Log.info logger (LogEvent.Create("wordsFromValue got {count} results", result.Length))

        result