module Services

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
open Interfaces
#endif

open Services
open Services.JS

type ConsoleLogger (category : string, logger : Interfaces.Logger) =
    interface Services.ILogger with
        member _.Log level event =
            let logEntry = { EventId = event.EventId; LogLevel = level; Category = category; Message = event.Message; State = event.Params }

            let json = logEntry |> LogEntry.toString 0 :> obj |> Some
            match level with
            | Critical
            | Error    -> logger.error   json
            | Warn     -> logger.warn    json
            | Info     -> logger.info    json
            | Trace
            | Debug    -> logger.verbose json




