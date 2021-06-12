module WordValues.Azure.Function

open System.Net
open System.Web
open System.Collections.Specialized
open Thoth.Json.Net

open Microsoft.Azure.Functions.Worker
open Microsoft.Azure.Functions.Worker.Http

open Services.Clr
open WordValues

module NameValueCollection =
    let tryGet (key : string) (nvc : NameValueCollection) =
        nvc.[key] |> Option.ofObj

[<Function("WordValue")>]
let run ([<HttpTrigger(AuthorizationLevel.Anonymous, "get")>] request:HttpRequestData, executionContext:FunctionContext) : HttpResponseData =
    let logger = MicrosoftLogger(executionContext.GetLogger("Function"))

    let wordParam =
        HttpUtility.ParseQueryString(request.Url.Query)
        |> NameValueCollection.tryGet "word"
    match wordParam with
    | Some word ->
        let result = Calculate.wordValue logger word
        let content = result |> WordValue.Encoder |> Encode.toString 0

        let response = request.CreateResponse(HttpStatusCode.OK)
        response.Headers.Add("Content-Type", "application/json")
        response.WriteString(content)
        response
    | None ->
        let response = request.CreateResponse(HttpStatusCode.BadRequest)
        response.Headers.Add("Content-Type", "text/plain;charset=utf-8")
        response.WriteString("Required query parameter 'word' was missing")
        response

