module WordValues.Azure.JS

open System
open System.Net
open Fable.Core
open Fable.Core.JsInterop
open Thoth.Json

open Interfaces
open Response
open Request

open WordValues

let inline encoder<'T> = Encode.Auto.generateEncoderCached<'T>(skipNullField = true)

let private run (context : Context) (request : HttpRequest) =
    let wordParam = request.query.["word"]

    let response = createEmpty<Response>
    response.headers <- createEmpty<IResponseHeaders> // Is initially null

    match wordParam with
    | Some word ->
        let result = Calculate.wordValue word
        let content = encoder result |> Encode.toString 4
        response.statusCode <- (HttpStatusCode.OK |> float |> U2<string, float>.op_ErasedCast |> Some)
        response.headers.["Content-Type"] <- ("application/json" :> obj |> Some)
        response.body <- (content :> obj |> Some)
    | None ->
        response.statusCode <- (HttpStatusCode.BadRequest |> float |> U2<string, float>.op_ErasedCast |> Some)
        response.headers.["Content-Type"] <- ("text/plain;charset=utf-8" :> obj |> Some)
        response.body <- ("Required query parameter 'word' was missing" :> obj |> Some)

    context.res <- Some (response :> ContextRes)
    context.``done`` ()

exportDefault (Action<_, _> run)
