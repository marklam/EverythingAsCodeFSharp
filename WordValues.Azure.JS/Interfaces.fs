// ts2fable 0.8.0-build.615
module rec Interfaces

#nowarn "3390" // disable warnings for invalid XML comments

open System
open Fable.Core
open Fable.Core.JS

type Error = System.Exception


/// Context bindings object. Provided to your function binding data, as defined in function.json.
type [<AllowNullLiteral>] ContextBindings =
    [<EmitIndexer>] abstract Item: name: string -> obj option with get, set

/// Context binding data. Provided to your function trigger metadata and function invocation data.
type [<AllowNullLiteral>] ContextBindingData =
    abstract invocationId: string option with get, set
    [<EmitIndexer>] abstract Item: name: string -> obj option with get, set

/// The context object can be used for writing logs, reading data from bindings, setting outputs and using 
/// the context.done callback when your exported function is synchronous. A context object is passed 
/// to your function from the Azure Functions runtime on function invocation.
type [<AllowNullLiteral>] Context =
    /// A unique GUID per function invocation.
    abstract invocationId: string with get, set
    /// Function execution metadata.
    abstract executionContext: ExecutionContext with get, set
    /// Input and trigger binding data, as defined in function.json. Properties on this object are dynamically 
    /// generated and named based off of the "name" property in function.json.
    abstract bindings: ContextBindings with get, set
    /// Trigger metadata and function invocation data.
    abstract bindingData: ContextBindingData with get, set
    /// TraceContext information to enable distributed tracing scenarios.
    abstract traceContext: TraceContext with get, set
    /// Bindings your function uses, as defined in function.json.
    abstract bindingDefinitions: ResizeArray<BindingDefinition> with get, set
    /// Allows you to write streaming function logs. Calling directly allows you to write streaming function logs 
    /// at the default trace level.
    abstract log: Logger with get, set
    /// <summary>
    /// A callback function that signals to the runtime that your code has completed. If your function is synchronous,
    /// you must call context.done at the end of execution. If your function is asynchronous, you should not use this 
    /// callback.
    /// </summary>
    /// <param name="err">A user-defined error to pass back to the runtime. If present, your function execution will fail.</param>
    /// <param name="result">
    /// An object containing output binding data. <c>result</c> will be passed to JSON.stringify unless it is
    /// a string, Buffer, ArrayBufferView, or number.
    /// </param>
    abstract ``done``: ?err: U2<Error, string> * ?result: obj -> unit
    /// HTTP request object. Provided to your function when using HTTP Bindings.
    abstract req: HttpRequest option with get, set
    /// HTTP response object. Provided to your function when using HTTP Bindings.
    abstract res: ContextRes option with get, set

/// HTTP request headers.
type [<AllowNullLiteral>] HttpRequestHeaders =
    [<EmitIndexer>] abstract Item: name: string -> string option with get, set

/// Query string parameter keys and values from the URL.
type [<AllowNullLiteral>] HttpRequestQuery =
    [<EmitIndexer>] abstract Item: name: string -> string option with get, set

/// Route parameter keys and values.
type [<AllowNullLiteral>] HttpRequestParams =
    [<EmitIndexer>] abstract Item: name: string -> string option with get, set

/// HTTP request object. Provided to your function when using HTTP Bindings.
type [<AllowNullLiteral>] HttpRequest =
    /// HTTP request method used to invoke this function.
    abstract method: HttpMethod option with get, set
    /// Request URL.
    abstract url: string with get, set
    /// HTTP request headers.
    abstract headers: HttpRequestHeaders with get, set
    /// Query string parameter keys and values from the URL.
    abstract query: HttpRequestQuery with get, set
    /// Route parameter keys and values.
    abstract ``params``: HttpRequestParams with get, set
    /// The HTTP request body.
    abstract body: obj option with get, set
    /// The HTTP request body as a UTF-8 string.
    abstract rawBody: obj option with get, set

/// Possible values for an HTTP request method.
type [<StringEnum>] [<RequireQualifiedAccess>] HttpMethod =
    | [<CompiledName "GET">] GET
    | [<CompiledName "POST">] POST
    | [<CompiledName "DELETE">] DELETE
    | [<CompiledName "HEAD">] HEAD
    | [<CompiledName "PATCH">] PATCH
    | [<CompiledName "PUT">] PUT
    | [<CompiledName "OPTIONS">] OPTIONS
    | [<CompiledName "TRACE">] TRACE
    | [<CompiledName "CONNECT">] CONNECT

/// Http response cookie object to "Set-Cookie"
type [<AllowNullLiteral>] Cookie =
    /// Cookie name
    abstract name: string with get, set
    /// Cookie value
    abstract value: string with get, set
    /// Specifies allowed hosts to receive the cookie
    abstract domain: string option with get, set
    /// Specifies URL path that must exist in the requested URL
    abstract path: string option with get, set
    /// NOTE: It is generally recommended that you use maxAge over expires.
    /// Sets the cookie to expire at a specific date instead of when the client closes.
    /// This can be a Javascript Date or Unix time in milliseconds.
    abstract expires: U2<DateTime, float> option with get, set
    /// Sets the cookie to only be sent with an encrypted request
    abstract secure: bool option with get, set
    /// Sets the cookie to be inaccessible to JavaScript's Document.cookie API
    abstract httpOnly: bool option with get, set
    /// Can restrict the cookie to not be sent with cross-site requests
    abstract sameSite: CookieSameSite option with get, set
    /// Number of seconds until the cookie expires. A zero or negative number will expire the cookie immediately.
    abstract maxAge: float option with get, set

type [<AllowNullLiteral>] ExecutionContext =
    /// A unique GUID per function invocation.
    abstract invocationId: string with get, set
    /// The name of the function that is being invoked. The name of your function is always the same as the
    /// name of the corresponding function.json's parent directory.
    abstract functionName: string with get, set
    /// The directory your function is in (this is the parent directory of this function's function.json).
    abstract functionDirectory: string with get, set
    /// The retry context of the current funciton execution. The retry context of the current function execution. Equals null if retry policy is not defined or it's the first function execution.
    abstract retryContext: RetryContext option with get, set

type [<AllowNullLiteral>] RetryContext =
    /// Current retry count of the function executions.
    abstract retryCount: float with get, set
    /// Max retry count is the maximum number of times an execution is retried before eventual failure. A value of -1 means to retry indefinitely.
    abstract maxRetryCount: float with get, set
    /// Exception that caused the retry
    abstract ``exception``: Exception option with get, set

type [<AllowNullLiteral>] Exception =
    /// Exception source
    abstract source: string option with get, set
    /// Exception stackTrace
    abstract stackTrace: string option with get, set
    /// Exception message
    abstract message: string option with get, set

/// TraceContext information to enable distributed tracing scenarios.
type [<AllowNullLiteral>] TraceContext =
    /// Describes the position of the incoming request in its trace graph in a portable, fixed-length format.
    abstract traceparent: string option with get, set
    /// Extends traceparent with vendor-specific data.
    abstract tracestate: string option with get, set
    /// Holds additional properties being sent as part of request telemetry.
    abstract attributes: TraceContextAttributes option with get, set

type [<AllowNullLiteral>] BindingDefinition =
    /// The name of your binding, as defined in function.json.
    abstract name: string with get, set
    /// The type of your binding, as defined in function.json.
    abstract ``type``: string with get, set
    /// The direction of your binding, as defined in function.json.
    abstract direction: BindingDefinitionDirection with get, set

/// Allows you to write streaming function logs.
type [<AllowNullLiteral>] Logger =
    /// Writes streaming function logs at the default trace level.
    [<Emit "$0($1...)">] abstract Invoke: [<ParamArray>] args: obj option[] -> unit
    /// Writes to error level logging or lower.
    abstract error: [<ParamArray>] args: obj option[] -> unit
    /// Writes to warning level logging or lower.
    abstract warn: [<ParamArray>] args: obj option[] -> unit
    /// Writes to info level logging or lower.
    abstract info: [<ParamArray>] args: obj option[] -> unit
    /// Writes to verbose level logging.
    abstract verbose: [<ParamArray>] args: obj option[] -> unit

type [<AllowNullLiteral>] ContextRes =
    [<EmitIndexer>] abstract Item: key: string -> obj option with get, set

type [<StringEnum>] [<RequireQualifiedAccess>] CookieSameSite =
    | [<CompiledName "Strict">] Strict
    | [<CompiledName "Lax">] Lax
    | [<CompiledName "None">] None

type [<AllowNullLiteral>] TraceContextAttributes =
    [<EmitIndexer>] abstract Item: k: string -> string with get, set

type [<StringEnum>] [<RequireQualifiedAccess>] BindingDefinitionDirection =
    | In
    | Out
    | Inout
