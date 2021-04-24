#if FAKE
#r """paket:
    source https://api.nuget.org/v3/index.json
    nuget FSharp.Core 4.7.2
    nuget Fake.Core.Target
    //"""
#endif

#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core

module Target =
    let create name description body =
        Target.description description
        Target.create name body
        name

let noop = Target.create "Noop" "Does nothing" ignore

// Default target
Target.runOrDefault noop