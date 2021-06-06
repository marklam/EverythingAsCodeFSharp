namespace Deployment.Tests.Azure

open System.IO

open Deployment.Tests

module private Deployment =
    let folder = Path.Combine(DirectoryInfo(__SOURCE_DIRECTORY__).Parent.FullName, "Deployment.Azure")
    let envVars = dict [ ]

type AzurePulumiStackInstance() =
    inherit PulumiStack("azure-dev", Deployment.folder, Deployment.envVars)


