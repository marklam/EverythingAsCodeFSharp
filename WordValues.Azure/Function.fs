module WordValues.Azure.Function

open System.Net
open Microsoft.Azure.Functions.Worker
open Microsoft.Azure.Functions.Worker.Http

[<Function("WordValue")>]
let run ([<HttpTrigger(AuthorizationLevel.Anonymous, "get")>] request:HttpRequestData, executionContext:FunctionContext) : HttpResponseData =
    let response = request.CreateResponse(HttpStatusCode.OK)
    response.Headers.Add("Content-Type", "text/plain; charset=utf-8")
    response.WriteString("Hello")
    response