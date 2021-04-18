namespace Deployment.Tests.Azure

open System
open System.IO
open System.Net

open FsHttp
open FsHttp.Dsl

open Xunit

open Deployment.Tests

module Deployment =
    let folder = Path.Combine(DirectoryInfo(__SOURCE_DIRECTORY__).Parent.FullName, "Deployment")

type AzurePulumiStackInstance() =
    inherit PulumiStack("dev", Deployment.folder)

// TODO - category for slow tests that require cloud function
type TestAzureFun (stack : AzurePulumiStackInstance) =
    interface IClassFixture<AzurePulumiStackInstance>

    [<Fact>]
    member _.``WordValue returns dummy Hello message`` () =
        let outputs = stack.GetOutputs()
        let testUri = Uri(outputs.["endpoint"].Value :?> string, UriKind.Absolute)

        let response =
            get testUri.AbsoluteUri
            |> Request.send

        Assert.Equal(HttpStatusCode.OK, response.statusCode)
        Assert.Equal("Hello", response |> Response.toText)