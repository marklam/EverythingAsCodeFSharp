namespace Deployment.Tests.Aws

open System

open Xunit

open Testing.Apis
open Deployment.Tests

// TODO - category for slow tests that require cloud function
[<Collection(TestCollections.AwsStack)>]
type TestAwsLambda (stack : AwsPulumiStackInstance) =
    inherit TestWordValueEndpoints(fun () -> Uri(stack.GetOutputs().["endpoint"].Value :?> string, UriKind.Absolute))
    interface IClassFixture<AwsPulumiStackInstance>
