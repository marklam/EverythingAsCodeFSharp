namespace WordValues.Azure.JS.Tests

open System
open System.IO
open System.Text.Json
open System.Net

open FsHttp
open FsHttp.Dsl

open Xunit

open WordValues.Azure.Tests

module WordValuesAzureJSFunc =
    let folder = Path.Combine(DirectoryInfo(__SOURCE_DIRECTORY__).Parent.FullName, "WordValues.Azure.JS")

type WordValuesAzureFuncInstance() =
    inherit AzureFuncInstance(WordValuesAzureJSFunc.folder, 7072)

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
    member _.``WordValue with no 'word' query parameter returns an error`` () =
        let connection = func.GetConnection()
        let testUri = Uri(connection.BaseUri, "/api/WordValue?spoons=3")

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
        Assert.Equal("Ignored ' ','1','2','3'", result.RootElement.GetProperty("Warning").GetString())
