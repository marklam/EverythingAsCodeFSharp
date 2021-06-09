module Program

open Pulumi.FSharp
open Pulumi.Github

let infra () =
    let repo =
        Repository(
            "EverythingAsCodeFSharp",
            RepositoryArgs(
                Name              = input "EverythingAsCodeFSharp",
                Description       = input "Cloud projects with devops, deployment and build all done in code. In F#.",
                Visibility        = input "public",
                GitignoreTemplate = input "VisualStudio",
                HasIssues         = input true
            )
        )

    let defaultBranch =
        Pulumi.Github.BranchDefault(
            "EverythingAsCodeFSharpDefaultBranch",
            BranchDefaultArgs(
                Repository = io repo.Name,
                Branch     = input "main"
            )
        )

    // Export outputs here
    dict [
        ("EverythingAsCodeFSharp.Clone", repo.HttpCloneUrl :> obj)
    ]

[<EntryPoint>]
let main _ =
  Deployment.run infra
