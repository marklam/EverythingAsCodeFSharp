## Searching downloaded CloudTrail logs
The CloudTrail Management Console's web page for Event History has only basic filtering - by user name etc.

To perform more interesting searches, and dump the results all in one list instead of having to look at each event individually, we can download the events as json. Using the Json Type Provider from FSharp.Data we get intellisense to help browse the properties of the logged events.

I downloaded a CloudWatch log file as json, and then used it as the example data for the type provider (I did this in Visual Studio Code with Ionide, because Visual Studio was having problems with type providers at the time of writing)
```fsharp
#r "nuget:FSharp.Data"
open FSharp.Data

type EventHistory = JsonProvider< @"c:\users\mark\downloads\event_history.json">
```
Now when an event file is loaded, the intellisense offers properties etc to filter the events on (after typing `events.`, `record.` or `r.` in the following example)
```fsharp
let events = EventHistory.Load @"c:\users\mark\downloads\event_history.json"

let accessDenied =
    events.Records
    |> Seq.filter (fun r -> r.ErrorCode = Some "AccessDenied")
    |> Seq.distinctBy (fun r -> r.ErrorMessage)

for record in accessDenied do
    printfn $"{record.ErrorMessage}"
```