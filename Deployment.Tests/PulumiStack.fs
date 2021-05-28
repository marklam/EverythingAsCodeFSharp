namespace Deployment.Tests

open FSharp.Control.Tasks

open Pulumi.Automation

type PulumiStack (stackName, folder, envVars) =
    let outputs =
        task {
            let  args    = LocalProgramArgs(stackName, folder, EnvironmentVariables = envVars)
            let! stack   = LocalWorkspace.SelectStackAsync(args)
            let! outputs = stack.GetOutputsAsync()
            return outputs
        }

    member _.GetOutputs() =
        outputs.Result

module TestCollections =
    let [<Literal>] AzureStack = "Azure Stack Tests"
    let [<Literal>] AwsStack   = "Aws Stack Tests"