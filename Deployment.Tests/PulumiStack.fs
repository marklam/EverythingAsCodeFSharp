namespace Deployment.Tests

open System
open FSharp.Control.Tasks

open Pulumi.Automation

type PulumiStack (stackName, folder, expectedEnvVars) =
    let missingEnvVars =
        let envVars = Environment.GetEnvironmentVariables()
        expectedEnvVars
        |> List.filter (not << envVars.Contains)

    let outputs =
        task {
            if (not <| List.isEmpty missingEnvVars) then
                missingEnvVars |> String.concat ", "
                |> failwithf "Missing environment variables: %s - set in environment or file specified in project's RunSettingsFilePath property"

            let  args    = LocalProgramArgs(stackName, folder)
            let! stack   = LocalWorkspace.SelectStackAsync(args)
            let! outputs = stack.GetOutputsAsync()
            return outputs
        }

    member _.GetOutputs() =
        outputs.Result

module TestCollections =
    let [<Literal>] AzureStack = "Azure Stack Tests"
    let [<Literal>] AwsStack   = "Aws Stack Tests"