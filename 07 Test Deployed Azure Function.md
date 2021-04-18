## Testing the Deployed Function
Again, testing the deployed function by copying URLs from the command prompt and pasting into a web client is a bit too manual.
It's time-consuming and easy to forget to do. So it's time to automate again.
## Pulumi.Automation
The Pulumi Automation sdk allows us to grab values from the stack output instead of having to copy-paste, and feed them into automated test.

```cmd
mkdir Deployment.Tests
cd Deployment.Tests
dotnet new library --language F#
```
Add a paket.references file for the same test assemblies used in the other test project (so they're all already in the paket.dependencies)
```
Microsoft.NET.Test.Sdk
SchlenkR.FsHttp
xunit
xunit.runner.visualstudio
coverlet.collector
FSharp.Core
```
And then add the Pulumi Automation Api to interact with our Pulumi stack
```cmd
dotnet paket add Pulumi.Automation --project Deployment.Tests
dotnet restore
```
Again using xUnit's [class fixtures](https://xunit.net/docs/shared-context#class-fixture) we can get the outputs from the Pulumi stack
```fsharp
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
```
Then in the test, we can grab the "endpoint" output from the `stack` fixture
```fsharp
        let outputs = stack.GetOutputs()
        let testUri = Uri(outputs.["endpoint"].Value :?> string, UriKind.Absolute)
```
And use FsHttp to test the deployed function.