module WordValues.Azure.Function

open System.Net
open System.Web
open System.Collections.Specialized
open System.Text.Json

open Microsoft.Azure.Functions.Worker
open Microsoft.Azure.Functions.Worker.Http

open WordValues

module NameValueCollection =
    let tryFind (key : string) (nvc : NameValueCollection) =
        nvc.[key] |> Option.ofObj

[<Function("WordValue")>]
let run ([<HttpTrigger(AuthorizationLevel.Anonymous, "get")>] request:HttpRequestData, executionContext:FunctionContext) : HttpResponseData =
    let parameters = HttpUtility.ParseQueryString(request.Url.Query)
    let wordParam = parameters |> NameValueCollection.tryFind "word"
    match wordParam with
    | Some word ->
        let result = Calculate.wordValue word
        let content = JsonSerializer.Serialize<_>(result, JsonSerializerOptions(IgnoreNullValues = true))

        let response = request.CreateResponse(HttpStatusCode.OK)
        response.Headers.Add("Content-Type", "application/json")
        response.WriteString(content)
        response
    | None ->
        let response = request.CreateResponse(HttpStatusCode.BadRequest)
        response.Headers.Add("Content-Type", "text/plain;charset=utf-8")
        response.WriteString("Required query parameter 'word' was missing")
        response

