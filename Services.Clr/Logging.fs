namespace Services.Clr

open Microsoft.Extensions.Logging

type MicrosoftLogger (logger : Microsoft.Extensions.Logging.ILogger) =
    interface Services.ILogger with
        member _.Log (level: Services.LogLevel) (event: Services.LogEvent) : unit =
            let logLevel =
                match level with
                | Services.LogLevel.Trace    -> LogLevel.Trace
                | Services.LogLevel.Debug    -> LogLevel.Debug
                | Services.LogLevel.Info     -> LogLevel.Information
                | Services.LogLevel.Warn     -> LogLevel.Warning
                | Services.LogLevel.Error    -> LogLevel.Error
                | Services.LogLevel.Critical -> LogLevel.Critical

            logger.Log(logLevel, EventId event.EventId, Unchecked.defaultof<exn>, event.Message, event.Params)
