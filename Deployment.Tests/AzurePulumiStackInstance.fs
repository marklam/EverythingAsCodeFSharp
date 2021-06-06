namespace Deployment.Tests.Azure

open System.IO

open Deployment.Tests

module private Deployment =
    let folder = Path.Combine(DirectoryInfo(__SOURCE_DIRECTORY__).Parent.FullName, "Deployment.Azure")
    let envVars = [ "PULUMI_CONFIG_PASSPHRASE"; "AWS_REGION"; "AWS_ACCESS_KEY_ID"; "AWS_SECRET_ACCESS_KEY" ]

type AzurePulumiStackInstance() =
    inherit PulumiStack("azure-dev", Deployment.folder, Deployment.envVars)


