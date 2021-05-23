#r "nuget:FSharp.Data"
open FSharp.Data

type EventHistory = JsonProvider< @"c:\users\mark\downloads\event_history.json">

let events = EventHistory.Load @"c:\users\mark\downloads\event_history.json"

let accessDenied =
    events.Records
    |> Seq.filter (fun r -> r.ErrorCode = Some "AccessDenied")
    |> Seq.distinctBy (fun r -> r.ErrorMessage)

for record in accessDenied do
    printfn $"{record.ErrorMessage}"
