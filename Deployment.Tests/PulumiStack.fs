namespace Deployment.Tests

open FSharp.Control.Tasks

open Pulumi.Automation

type PulumiStack (stackName, folder) =
    let outputs =
        task {
            let  args    = LocalProgramArgs(stackName, folder)
            let! stack   = LocalWorkspace.SelectStackAsync(args)
            let! outputs = stack.GetOutputsAsync()
            return outputs
        }

    member _.GetOutputs() =
        outputs.Result

