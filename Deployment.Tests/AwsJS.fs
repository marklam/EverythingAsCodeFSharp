namespace Deployment.Tests.Aws

open System

open Xunit

open Deployment.Tests

// TODO - category for slow tests that require cloud function
[<Collection("Aws stack tests")>]
type TestAwsJSLambda (stack : AwsPulumiStackInstance) =
    inherit TestWordValueEndpoints(fun () -> Uri(stack.GetOutputs().["jsEndpoint"].Value :?> string, UriKind.Absolute))
    interface IClassFixture<AwsPulumiStackInstance>
