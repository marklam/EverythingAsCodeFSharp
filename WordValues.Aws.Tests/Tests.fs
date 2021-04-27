namespace WordValues.Aws.Tests

open System
open System.Net
open System.Text.Json

open Xunit
open Amazon.Lambda.TestUtilities
open WordValues.Aws

module FunctionTest =

    [<Fact>]
    let ``WordValue with no query parameter returns an error`` () =
        let context = TestLambdaContext()
        let response = Function.functionHandler null context

        Assert.Equal(int HttpStatusCode.BadRequest, response.StatusCode)
        Assert.Equal("Required query parameter 'word' was missing", response.Body)

    [<Fact>]
    let ``WordValue returns the correct message`` () =
        let context = TestLambdaContext()

        let response = Function.functionHandler "Hello" context

        Assert.Equal(int HttpStatusCode.OK, response.StatusCode)
        Assert.Equal("""{"Value":52}""", response.Body)

    [<Fact>]
    let ``WordValue returns warnings for non-letters`` () =
        let context = TestLambdaContext()

        let response = Function.functionHandler "Hello 123" context

        Assert.Equal(int HttpStatusCode.OK, response.StatusCode)

        let result = response.Body |> JsonDocument.Parse
        Assert.Equal(52, result.RootElement.GetProperty("Value").GetInt32())
        Assert.Equal("Ignored ' ','1','2','3'", result.RootElement.GetProperty("Warning").GetProperty("Value").GetString())
