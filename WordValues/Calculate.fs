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
