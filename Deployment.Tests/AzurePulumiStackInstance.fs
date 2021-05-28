namespace Deployment.Tests.Azure

open System.IO

open Deployment.Tests

module private Deployment =
    let folder = Path.Combine(DirectoryInfo(__SOURCE_DIRECTORY__).Parent.FullName, "Deployment")
    let envVars = dict [ "PULUMI_CONFIG_PASSPHRASE", "My secure passphrase" ]

type AzurePulumiStackInstance() =
    inherit PulumiStack("dev", Deployment.folder, Deployment.envVars)


