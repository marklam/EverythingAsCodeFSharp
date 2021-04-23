namespace WordValues.Azure.Tests

open System
open System.Text.Json
open System.Net

open FsHttp
open FsHttp.Dsl

open Xunit

type WordValuesAzureFuncInstance() =
    inherit AzureFuncInstance(typeof<WordValuesAzureFuncInstance>, <@ WordValues.Azure.Program.main @>, 7071)

// TODO - category for slow tests that require func.exe
type TestAzureFun (func : WordValuesAzureFuncInstance) =
    interface IClassFixture<WordValuesAzureFuncInstance>

    [<Fact>]
    member _.``WordValue with no query parameter returns an error`` () =
        let connection = func.GetConnection()
        let testUri = Uri(connection.BaseUri, "/api/WordValue")

        let response =
            get testUri.AbsoluteUri
            |> Request.send

        Assert.Equal(HttpStatusCode.BadRequest, response.statusCode)
        Assert.Equal("Required query parameter 'word' was missing", response |> Response.toText)

    [<Fact>]
    member _.``WordValue returns the correct message`` () =
        let connection = func.GetConnection()
        let testUri = Uri(connection.BaseUri, "/api/WordValue?word=Hello")

        let response =
            get testUri.AbsoluteUri
            |> Request.send

        Assert.Equal(HttpStatusCode.OK, response.statusCode)
        Assert.Equal("""{"Value":52}""", response |> Response.toText)

    [<Fact>]
    member _.``WordValue returns warnings for non-letters`` () =
        let connection = func.GetConnection()
        let testUri = Uri(connection.BaseUri, "/api/WordValue?word=" + Uri.encodeUrlParam "Hello 123")

        let response =
            get testUri.AbsoluteUri
            |> Request.send

        Assert.Equal(HttpStatusCode.OK, response.statusCode)

        let result = response |> Response.toText |> JsonDocument.Parse
        Assert.Equal(52, result.RootElement.GetProperty("Value").GetInt32())
        Assert.Equal("Ignored ' ','1','2','3'", result.RootElement.GetProperty("Warning").GetProperty("Value").GetString())
