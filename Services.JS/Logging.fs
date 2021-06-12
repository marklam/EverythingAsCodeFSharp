namespace Services.JS

open System.Text.RegularExpressions

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

type LogEntry = { EventId : int; LogLevel : Services.LogLevel; Category : string; Message : string; State : obj[] }
    with
        static member Encoder (e : LogEntry) =
            let level =
                match e.LogLevel with
                | Services.LogLevel.Trace    -> "Trace"
                | Services.LogLevel.Debug    -> "Debug"
                | Services.LogLevel.Info     -> "Information"
                | Services.LogLevel.Warn     -> "Warning"
                | Services.LogLevel.Error    -> "Error"
                | Services.LogLevel.Critical -> "Critical"

            let namesAndValues =
                (Regex.Matches(e.Message, "{([^}]+)}"), e.State)
                ||> Seq.map2 (fun m v -> (m.Groups.[1].Value, Encode.Auto.generateEncoder() v))
                 |> List.ofSeq

            let replacer =
                let map = namesAndValues |> Map.ofList
                fun (m : Match)  -> map.[m.Groups.[1].Value] |> Encode.toString 0

            let message =
                Regex.Replace(e.Message, "{([^}]+)}", replacer)

            let state =
                namesAndValues
                |> List.append [("Message", Encode.string message); ("{OriginalFormat}", Encode.string e.Message) ]
                |> Encode.object

            Encode.object [
                ("EventId", Encode.int e.EventId)
                ("LogLevel", Encode.string level)
                ("Category", Encode.string e.Category)
                ("Message", Encode.string message)
                ("State", state)
            ]

module LogEntry =
    let toString n e = e |> LogEntry.Encoder |> Encode.toString n
