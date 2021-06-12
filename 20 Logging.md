## Logging
Because debugging cloud services isn't (usually?) a case of starting a debugger and single-stepping the code, I want to have logging in the services to see what was going on if anything breaks.

Because the logic is independent of which cloud it's in (and even whether it's in the cloud at all), I want to make an interface for the logger and some F# functions to do the logging.

### The basic version
I created an F# project called "Services" and added:
* an interface for logging which doesn't depend on any particular logging framework (although it's based on `Microsoft.Extensions.Logging`)
* an F# module for logging things
* a type for the logged events, to get away from the function overloading style.
```fsharp
namespace Services

open System

type LogLevel =
    | Trace
    | Debug
    | Info
    | Warn
    | Error
    | Critical

[<Struct>]
type LogEvent = { Message : string; EventId : int; Params : obj[] } with
    static member Create(message, [<ParamArray>] pars) = { Message = message; EventId = 0; Params = pars }

type ILogger =
    abstract Log : LogLevel -> LogEvent -> unit

module Log =
    let info (logger : ILogger) event =
        logger.Log Info event
```
(I'll add other Logging level functions later).
### Using the logger
Initially the level was in the event, but this layout made the function usage look nicer - like:
```fsharp
    let wordValue (logger : ILogger) (text : string) : WordValue =
        Log.info logger (LogEvent.Create("wordValue of {text}", text))
```
The message / parameter syntax is the same as `Microsoft.Extensions.Logging` uses, and there are three things I dislike about it:
1. `("one is {one} and two is two", one, two)` will lose the value of `two` because of the missing brackets
2. `("one is {one} and two is {two}", one)` will throw at runtime because no parameter is supplied for the second value
3. `("one is {one} and two is {two}", two, one)` will produce misleading results because the parameters don't match the string.

1 and 2 can be at mitigated against by having the test methods use a logger implementation that just checks for this sort of error, rather than a dumb mock object that just ignores logging requests.
### Implementation
* Azure Functions in .net can use the `Microsoft.Extensions.Logging.ILogger<_>_` from Dependency Injection
* AWS Lambdas in .net can use the (misleadingly named) `Amazon.Lambda.Logging.AspNetCore` nuget package.

For these two, I created a Services.Clr project which implements the ILog interface in terms of `Microsoft.Extensions.Logging.ILogger`

* Azure Functions in Javascript can use the `Context.log` interface supplied to the function, which has logging functions for `info`, `error` etc.
* Aws Lambdas in Javascript can use console logging (so `System.Console.WriteLine` from Fable).

Neither of these methods support structured logging, so I created a `Services.JS` project to hold a Json encoder for the state passed to the `ILog` implementations.
### Test implementation
I added a `TestLogger` class and a singleton `TestLogger.Default` instance to use from the tests. Then because `TestLogger.Default` would be the first parameter to all the calls to `Calculate.wordValue`, I added a local `Calculate.wordValue` which partially bound that parameter ~~to make the diffs simpler~~ out of laziness.
```fsharp
// Partially bind the Testing Logger implementation
module Calculate =
    let wordValue = Calculate.wordValue (TestLogger.Default)
```
### Deployment
I added a target to the `build.fsx` Fake script that can be used to publish all the functions / lambdas. That is as simple as making a target that does nothing (so, using `ignore` as the body) and listing the publish targets as dependencies.
```fsharp
let publishAll =
    Target.create "PublishAll" "Publish all the Functions and Lambdas" ignore

publishAzureFunc   ==> publishAll
publishAzureJSFunc ==> publishAll
publishAwsLambda   ==> publishAll
publishAwsJSLambda ==> publishAll
```
I also found that I had forgotten to ask yarn to install the packages as part of the Javascript builds, and that I'd wrongly assumed that the `Dotnet.exec` and `Proc.run` tasks would fail the build on a non-zero exit code from the tool - so I fixed those too.
```fsharp
type ProcessHelpers =
    static member checkResult (p : ProcessResult) =
        if p.ExitCode <> 0
        then failwithf "Expected exit code 0, but was %d" p.ExitCode

    static member checkResult (p : ProcessResult<_>) =
        if p.ExitCode <> 0
        then failwithf "Expected exit code 0, but was %d" p.ExitCode
```
```diff
         let projectFolder = solutionFolder </> "WordValues.Azure.JS"
+        let yarnParams (opt : Yarn.YarnParams) = { opt with WorkingDirectory = projectFolder }
-        DotNet.exec dotNetOpt "fable" "WordValues.Azure.JS" |> ignore
+        DotNet.exec dotNetOpt "fable" "WordValues.Azure.JS" |> ProcessHelpers.checkResult
+        Yarn.install yarnParams
-        Yarn.exec "build" (fun opt -> { opt with WorkingDirectory = projectFolder })
+        Yarn.exec "build" yarnParams
```
Once that was deployed, I tested the functions / lambdas in the Azure Portal / Aws Console and checked that the console output logging saw the info messages.