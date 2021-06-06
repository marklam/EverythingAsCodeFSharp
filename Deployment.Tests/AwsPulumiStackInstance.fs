namespace Deployment.Tests.Aws

open System.IO

open Deployment.Tests

module private Deployment =
    let folder = Path.Combine(DirectoryInfo(__SOURCE_DIRECTORY__).Parent.FullName, "Deployment.Aws")
    let envVars = dict [ ]

type AwsPulumiStackInstance() =
    inherit PulumiStack("aws-dev", Deployment.folder, Deployment.envVars)

