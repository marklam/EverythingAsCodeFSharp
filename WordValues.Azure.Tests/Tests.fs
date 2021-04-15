namespace WordValues.Azure.Tests

open System
open System.IO
open System.Net

open FsHttp
open FsHttp.Dsl

open Xunit

module WordValuesAzureFunc =
    let folder = Path.Combine(DirectoryInfo(__SOURCE_DIRECTORY__).Parent.FullName, "WordValues.Azure")

type WordValuesAzureFuncInstance() =
    inherit AzureFuncInstance(WordValuesAzureFunc.folder, 7071)

// TODO - category for slow tests that require func.exe
type TestAzureFun (func : WordValuesAzureFuncInstance) =
    interface IClassFixture<WordValuesAzureFuncInstance>

    [<Fact>]
    member _.``WordValue returns dummy Hello message`` () =
        use connection = func.GetConnection()
        let testUri = Uri(connection.BaseUri, "/api/WordValue")

        let response =
            get testUri.AbsoluteUri
            |> Request.send

        Assert.Equal(HttpStatusCode.OK, response.statusCode)
        Assert.Equal("Hello", response |> Response.toText)