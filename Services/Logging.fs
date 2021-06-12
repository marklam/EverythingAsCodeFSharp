namespace Services

open System

type LogLevel =
    | Trace
    | Debug
    | Info
    | Warn
    | Error
    | Critical

[<Struct>]
type LogEvent = { Message : string; EventId : int; Params : obj[] } with
    static member Create(message, [<ParamArray>] pars) = { Message = message; EventId = 0; Params = pars }

type ILogger =
    abstract Log : LogLevel -> LogEvent -> unit

module Log =
    let info (logger : ILogger) event =
        logger.Log Info event
