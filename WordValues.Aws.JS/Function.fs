module WordValues.Aws.JS

open System.Net
open Thoth.Json
open Fable.Core.JsInterop

open Amazon.Lambda.APIGatewayEvents.Request
open Amazon.Lambda.APIGatewayEvents.Response

open WordValues

let functionHandler (request : APIGatewayProxyRequest, _) =
    promise {
        let wordParam =
            request.queryStringParameters
            |> Option.bind (fun qsps -> qsps.["word"])

        let response = createEmpty<APIGatewayProxyResponse>
        response.headers <- createEmpty<Headers>

        match wordParam with
        | Some word ->
            let result = Calculate.wordValue word
            let content = result |> WordValue.Encoder |> Encode.toString 0

            response.statusCode <- int HttpStatusCode.OK
            response.headers.["Content-Type"] <- Some "application/json"
            response.body <- content
        | None ->
            response.statusCode <- int HttpStatusCode.BadRequest
            response.headers.["Content-Type"] <- Some "text/plain;charset=utf-8"
            response.body <- "Required query parameter 'word' was missing"

        return response
    }
