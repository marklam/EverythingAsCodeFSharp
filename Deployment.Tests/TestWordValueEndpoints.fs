namespace Deployment.Tests

open System
open System.Net
open System.Text.Json

open FsHttp
open FsHttp.Dsl

open Xunit

[<AbstractClass>]
type TestWordValueEndpoints (getEndpointUrl : unit -> Uri)  =
    [<Fact>]
    member _.``WordValue with no query parameter returns an error`` () =
        let endpoint = getEndpointUrl()
        let testUri = endpoint

        let response =
            get testUri.AbsoluteUri
            |> Request.send

        Assert.Equal(HttpStatusCode.BadRequest, response.statusCode)
        Assert.Equal("Required query parameter 'word' was missing", response |> Response.toText)

    [<Fact>]
    member _.``WordValue returns the correct message`` () =
        let endpoint = getEndpointUrl()
        let testUri = Uri(endpoint, "?word=Hello")

        let response =
            get testUri.AbsoluteUri
            |> Request.send

        Assert.Equal(HttpStatusCode.OK, response.statusCode)
        Assert.Equal("""{"Value":52}""", response |> Response.toText)

    [<Fact>]
    member _.``WordValue returns warnings for non-letters`` () =
        let endpoint = getEndpointUrl()
        let testUri = Uri(endpoint, "?word=" + Uri.encodeUrlParam "Hello 123")

        let response =
            get testUri.AbsoluteUri
            |> Request.send

        Assert.Equal(HttpStatusCode.OK, response.statusCode)

        let result = response |> Response.toText |> JsonDocument.Parse
        Assert.Equal(52, result.RootElement.GetProperty("Value").GetInt32())
        Assert.Equal("Ignored ' ','1','2','3'", result.RootElement.GetProperty("Warning").GetString())
