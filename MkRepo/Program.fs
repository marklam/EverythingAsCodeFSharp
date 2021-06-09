module Program

open Pulumi.FSharp
open Pulumi.Github

let infra () =
    let repo =
        Repository(
            "EverythingAsCodeFSharp",
            RepositoryArgs(
                Name = input "EverythingAsCodeFSharp",
                Description = input "Cloud projects with devops, deployment and build all done in code. In F#.",
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
