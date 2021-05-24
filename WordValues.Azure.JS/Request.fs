// ts2fable 0.8.0-build.615
module rec Request
open System
open Fable.Core
open Fable.Core.JS

type HttpRequest = Interfaces.HttpRequest
type HttpMethod = Interfaces.HttpMethod

type [<AllowNullLiteral>] IExports =
    abstract RequestProperties: RequestPropertiesStatic
    abstract Request: RequestStatic

type [<AllowNullLiteral>] RequestProperties =
    inherit HttpRequest
    abstract method: HttpMethod option with get, set
    abstract url: string with get, set
    abstract originalUrl: string with get, set
    abstract headers: RequestPropertiesHeaders with get, set
    abstract query: RequestPropertiesHeaders with get, set
    abstract ``params``: RequestPropertiesHeaders with get, set
    abstract body: obj option with get, set
    abstract rawBody: obj option with get, set
    [<EmitIndexer>] abstract Item: key: string -> obj option with get, set

type [<AllowNullLiteral>] RequestPropertiesStatic =
    [<EmitConstructor>] abstract Create: unit -> RequestProperties

type [<AllowNullLiteral>] Request =
    inherit RequestProperties
    abstract get: field: string -> string option

type [<AllowNullLiteral>] RequestStatic =
    [<EmitConstructor>] abstract Create: httpInput: RequestProperties -> Request

type [<AllowNullLiteral>] RequestPropertiesHeaders =
    [<EmitIndexer>] abstract Item: key: string -> string with get, set
