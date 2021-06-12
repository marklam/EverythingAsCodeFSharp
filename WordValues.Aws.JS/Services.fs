module Services

open Services.JS

type ConsoleLogger (category : string) =
    interface Services.ILogger with
        member _.Log level event =
            let logEntry = { EventId = event.EventId; LogLevel = level; Category = category; Message = event.Message; State = event.Params }

            let json = logEntry |> LogEntry.toString 0
            System.Console.WriteLine(json.ToString())

