namespace Deployment.Tests.Azure

open System

open Xunit

open Deployment.Tests

// TODO - category for slow tests that require cloud function
type TestAzureFunc (stack : AzurePulumiStackInstance) =
    inherit TestWordValueEndpoints(fun () -> Uri(stack.GetOutputs().["endpoint"].Value :?> string, UriKind.Absolute))
    interface IClassFixture<AzurePulumiStackInstance>
