module Program

open Pulumi.FSharp
open Pulumi.Github

let infra () =
    let repo =
        Repository(
            "EverythingAsCodeFSharp",
            RepositoryArgs(
                Name = input "EverythingAsCodeFSharp",
                Description = input "Generated from MkRepo",
                Visibility = input "private",
                GitignoreTemplate = input "VisualStudio"
            )
        )

    // Export outputs here
    dict [ 
        ("EverythingAsCodeFSharp.Clone", repo.HttpCloneUrl :> obj) 
    ]

[<EntryPoint>]
let main _ =
  Deployment.run infra
