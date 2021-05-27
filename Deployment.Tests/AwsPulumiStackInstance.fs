namespace Deployment.Tests.Aws

open System.IO

open Deployment.Tests

module private Deployment =
    let folder = Path.Combine(DirectoryInfo(__SOURCE_DIRECTORY__).Parent.FullName, "Deployment.Aws")
    let envVars = dict [ "PULUMI_CONFIG_PASSPHRASE", "My secure passphrase" ]

type AwsPulumiStackInstance() =
    inherit PulumiStack("dev", Deployment.folder, Deployment.envVars)

