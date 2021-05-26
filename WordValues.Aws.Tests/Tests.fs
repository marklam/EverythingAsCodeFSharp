namespace WordValues.Aws.Tests

open System.Net
open System.Text.Json

open Xunit
open WordValues.Aws
open Amazon.Lambda.APIGatewayEvents

module FunctionTest =

    [<Fact>]
    let ``WordValue with no query parameter returns an error`` () =
        let request = APIGatewayProxyRequest()

        let response = Function.functionHandler request

        Assert.Equal(int HttpStatusCode.BadRequest, response.StatusCode)
        Assert.Equal("Required query parameter 'word' was missing", response.Body)

    [<Fact>]
    let ``WordValue with no 'word' query parameter returns an error`` () =
        let request = APIGatewayProxyRequest(QueryStringParameters = dict["spoons", "3"])

        let response = Function.functionHandler request

        Assert.Equal(int HttpStatusCode.BadRequest, response.StatusCode)
        Assert.Equal("Required query parameter 'word' was missing", response.Body)

    [<Fact>]
    let ``WordValue returns the correct message`` () =
        let request = APIGatewayProxyRequest(QueryStringParameters = dict["word", "Hello"])

        let response = Function.functionHandler request

        Assert.Equal(int HttpStatusCode.OK, response.StatusCode)
        Assert.Equal("""{"Value":52}""", response.Body)

    [<Fact>]
    let ``WordValue returns warnings for non-letters`` () =
        let request = APIGatewayProxyRequest(QueryStringParameters = dict["word", "Hello 123"])

        let response = Function.functionHandler request

        Assert.Equal(int HttpStatusCode.OK, response.StatusCode)

        let result = response.Body |> JsonDocument.Parse
        Assert.Equal(52, result.RootElement.GetProperty("Value").GetInt32())
        Assert.Equal("Ignored ' ','1','2','3'", result.RootElement.GetProperty("Warning").GetString())
