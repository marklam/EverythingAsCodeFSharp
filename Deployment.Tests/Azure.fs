namespace Deployment.Tests.Azure

open System

open Xunit

open Testing.Apis
open Deployment.Tests

// TODO - category for slow tests that require cloud function
[<Collection(TestCollections.AzureStack)>]
type TestAzureFunc (stack : AzurePulumiStackInstance) =
    inherit TestWordValueEndpoints(fun () -> Uri(stack.GetOutputs().["endpoint"].Value :?> string, UriKind.Absolute))
    interface IClassFixture<AzurePulumiStackInstance>
