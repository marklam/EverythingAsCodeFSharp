namespace Deployment.Tests.Aws

open System.IO

open Deployment.Tests

module private Deployment =
    let folder = Path.Combine(DirectoryInfo(__SOURCE_DIRECTORY__).Parent.FullName, "Deployment.Aws")
    let envVars = [ "PULUMI_CONFIG_PASSPHRASE"; "AWS_REGION"; "AWS_ACCESS_KEY_ID"; "AWS_SECRET_ACCESS_KEY" ]

type AwsPulumiStackInstance() =
    inherit PulumiStack("aws-dev", Deployment.folder, Deployment.envVars)

