// ts2fable 0.8.0-build.615
module rec Response
open System
open Fable.Core
open Fable.Core.JS

type Function = System.Action

type Cookie = ___public_Interfaces.Cookie

type [<AllowNullLiteral>] IExports =
    abstract Response: ResponseStatic

type [<AllowNullLiteral>] IResponse =
    abstract statusCode: U2<string, float> option with get, set
    abstract headers: IResponseHeaders with get, set
    abstract cookies: ResizeArray<Cookie> with get, set
    abstract body: obj option with get, set
    abstract get: field: string -> obj option
    abstract set: field: string * ``val``: obj option -> IResponse
    abstract header: field: string * ``val``: obj option -> IResponse
    abstract status: statusCode: U2<string, float> -> IResponse

type [<AllowNullLiteral>] Response =
    inherit IResponse
    abstract statusCode: U2<string, float> option with get, set
    abstract headers: IResponseHeaders with get, set
    abstract cookies: ResizeArray<Cookie> with get, set
    abstract body: obj option with get, set
    abstract enableContentNegotiation: bool option with get, set
    [<EmitIndexer>] abstract Item: key: string -> obj option with get, set
    abstract ``end``: ?body: obj -> unit
    abstract setHeader: field: string * ``val``: obj option -> IResponse
    abstract getHeader: field: string -> IResponse
    abstract removeHeader: field: string -> unit
    abstract status: statusCode: U2<string, float> -> IResponse
    abstract sendStatus: statusCode: U2<string, float> -> unit
    abstract ``type``: ``type``: obj -> unit
    abstract json: body: obj -> unit
    abstract send: obj with get, set
    abstract header: obj with get, set
    abstract set: obj with get, set
    abstract get: obj with get, set

type [<AllowNullLiteral>] ResponseStatic =
    [<EmitConstructor>] abstract Create: ``done``: Function -> Response

type [<AllowNullLiteral>] IResponseHeaders =
    [<EmitIndexer>] abstract Item: key: string -> obj option with get, set
