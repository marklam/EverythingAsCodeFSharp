## Using the same worker code from JavaScript
The 'work' done by this Azure Function is trivial, and maybe a .net core function is overkill. A JavaScript function could do the same job, but since my aim here is to show **everything** as code (**in F#**) and not *JavaScript*, I'll use [Fable](https://fable.io/docs/) to generate JavaScript.

### Getting started
We'll need 
- Fable as a command-line tool
- The magic json files for Azure Functions to host the function (created with `func new`)
- A new project for Fable to compile into JavaScript (created with `dotnet new`)
- The new project to use the WordValues libary code
So, from the command prompt:
```cmd
dotnet tool install fable
mkdir WordValues.Azure.JS
cd WordValues.Azure.JS
func new -l JavaScript -n WordValues -t "HTTP trigger" --worker-runtime node
```
(I later renamed WordValues to WordValue because that name is used for the function Url, and the other functions/lambdas used 'WordValue')
```cmd
dotnet new classlib --language F# --framework netstandard2.1
dotnet add reference ..\WordValues\WordValues.fsproj
dotnet paket add FSharp.Core --project WordValues.Azure.JS
dotnet paket add Fable.Core --project WordValues.Azure.JS
dotnet paket add Thoth.Json --project WordValues.Azure.JS
```
I didn't have the node runtime, or a package manager for the nodejs side of things, so using Chocolatey again from an admin PowerShell prompt:
```powershell
choco install nodejs-lts
choco install yarn
```
I found that I needed the lts version of node (currently 14) so that Azure's `func` hosting would work with it: https://aka.ms/functions-node-versions
### Talking to the Azure Function hosting
Although the skeleton JavaScript function that the `func new` had created for me looked straightforward, Fable doesn't know anything about the types without a library for "the Node.js worker for the Azure Functions runtime". There didn't seem to be a ready-made one on NuGet.org, but while searching I found that [Florian Verdonck](https://github.com/nojaf) [had already done exactly what I was attempting](https://github.com/nojaf/refactor-2021/tree/master/src/server/nodejs/Fibonacci).

But since this was my first non-trivial adventure with TypeScript interop in Fable, I wanted to  understand how it all fits together. So I found the [Azure worker source repo](https://github.com/Azure/azure-functions-nodejs-worker) and tried creating my own.

I added the [ts2fable](https://github.com/fable-compiler/ts2fable) tool as a global tool using yarn, and ran it over the 3 files that it looked like I'd need from the [Azure function worker](https://github.com/Azure/azure-functions-nodejs-worker).
```cmd
yarn global add ts2fable
mkdir \temp\azurefunc
cd \temp\azurefunc
git clone https://github.com/Azure/azure-functions-nodejs-worker.git
ts2fable azure-functions-nodejs-worker\src\public\Interfaces.ts Interfaces.fs
ts2fable azure-functions-nodejs-worker\src\http\Request.ts Request.fs
ts2fable azure-functions-nodejs-worker\src\http\Response.ts Response.fs
```
[Zaid Ajaj](https://github.com/Zaid-Ajaj)'s article  [F# Interop with Javascript in Fable: The Complete Guide](https://medium.com/@zaid.naom/f-interop-with-javascript-in-fable-the-complete-guide-ccc5b896a59f) has lots of great detail about how to use the ts2fable-generated code.

I copied the 3 `.fs` files into the `WordValues.Azure.JS` project folder, added them to the class library project, and added a `Function.fs` file based on the version in `WordValues.Azure`.
```fsharp
let run (context : Context) (request : HttpRequest) =
    ...
    context.``done`` ()

exportDefault (Action<_, _> run)    
```
### Building the JavaScript
First I checked that the code would build as an F# project in Visual Studio.

Next I built it with fable:
```cmd
dotnet fable
```
When they were both successful, and had created some `.fs.js` versions of the source, I looked into bundling the JavaScript sources into one file to replace the `index.js` file in the skeleton project created by `func new`.

I found a recommendation for [parcel](https://parceljs.org/getting_started.html) as a straightforward bundling tool. I followed the instructions to install it:
```cmd
yarn add parcel-bundler --dev
```
And modified the `packages.json` file in the WordValues.Azure.JS project:
   
```diff    
 {
   "name": "",
   "version": "",
   "description": "",
   "scripts": {
+    "build": "parcel build Function.fs.js --out-dir WordValues --out-file index.js",
     "test": "echo \"No tests yet...\""
   },
   "author": "",
   "devDependencies": {
     "parcel-bundler": "^1.12.5"
   }
 }
```
I then built it, which overwrote the WordValues\index.js file with the bundled Fable output, which could be hosted by the Azure `func` tool:
```cmd
yarn build
func start
```
So that I didn't need to remember to run fable, then yarn & parcel - I created a Fake build target similar to the ones for publishing the .net Azure Function and Aws Lambda.
```fsharp
let publishAzureJSFunc =
    Target.create "PublishAzureJSFunc" "Publish the Azure Function as Javascript" (fun _ ->
        let projectFolder = solutionFolder </> "WordValues.Azure.JS"
        DotNet.exec dotNetOpt "fable" "WordValues.Azure.JS" |> ignore
        Yarn.exec "build" (fun opt -> { opt with WorkingDirectory = projectFolder })
    )
```
I tested the locally-hosted function and tweaked the bits I needed to make the function work properly:
- Create a `Response`
- Initialize `response.headers`
- Change the ts2fable-generated code to make `Response` inherit `ContextRes`, so it can be used as the `context.res` which is the result of the request.
### Deployment
While adding the Azure Function to the Pulumi script, I also made the Fake script build the zip file to avoid packaging any of the Fable project, and I renamed the WordValues folder to WordValue to make the Azure Function Url match the others.
```diff
 let publishAzureJSFunc =
     Target.create "PublishAzureJSFunc" "Publish the Azure Function as Javascript" (fun _ ->
         let projectFolder = solutionFolder </> "WordValues.Azure.JS"
         DotNet.exec dotNetOpt "fable" "WordValues.Azure.JS" |> ignore
         Yarn.exec "build" (fun opt -> { opt with WorkingDirectory = projectFolder })
+        let publishZip = System.IO.Path.Combine(projectFolder, "publish.zip")
+        let zipFiles =
+            !! (projectFolder </> "WordValue/**/*.*")
+            ++ (projectFolder </> "host.json")
+        Fake.IO.Zip.createZip projectFolder publishZip "" Fake.IO.Zip.DefaultZipLevel false zipFiles
     )
```
The deployment of the JavaScript function was very similar to the .net function:
```fsharp
    let jsApp =
        let appName = Random.decorate "jsapp"

        let siteConfig =
            Inputs.SiteConfigArgs(
                AppSettings =
                    InputList.ofNamedInputValues [
                        ("APPINSIGHTS_INSTRUMENTATIONKEY",           io appInsights.InstrumentationKey)
                        ("AzureWebJobsStorage",                      io storageConnection)
                        ("FUNCTIONS_EXTENSION_VERSION",              input "~3")
                        ("FUNCTIONS_WORKER_RUNTIME",                 input "node")
                        ("WEBSITE_CONTENTAZUREFILECONNECTIONSTRING", io storageConnection)
                        ("WEBSITE_CONTENTSHARE",                     io appName)
                        ("WEBSITE_RUN_FROM_PACKAGE",                 io jsCodeBlobUrl)
                        ("WEBSITE_NODE_DEFAULT_VERSION",             input "~14")
                    ],
                Http20Enabled = input true,
                NodeVersion   = input "~14"
            )

        WebApp(
            "jsapp",
            WebAppArgs(
                Name              = io appName,
                Kind              = input "FunctionApp",
                ResourceGroupName = io resourceGroup.Name,
                ServerFarmId      = io appServicePlan.Id,
                SiteConfig        = input siteConfig
            )
        )
```
So the differences are `FUNCTIONS_WORKER_RUNTIME` and `WEBSITE_NODE_DEFAULT_VERSION` environment variables, and the `Http20Enabled` and `NodeVersion` properties. 
The `jsCodeBlobUrl` is created the same way as the existing `codeBlobUrl`, and the uploaded zip was built by the Fake `PublishAzureJSFunc` target.
### Testing
I adapted the WordValues.Azure.Tests project to test the JavaScript version, as WordValues.Azure.JS.Tests - in the process I found an infinite loop in the constructors where a constructor `(string * int)` was calling itself instead of the constructor `(string * int * string option)`. 
```fsharp
type AzureFuncInstance private (folder, port, ?extraFuncExeParams) =
    ....
    new (folder, port) = new AzureFuncInstance(folder, port)
```
which was a silly mistake that I couldn't have made with functions rather than class members.

On running the tests, I also found a difference in the json being returned, where a Warning was passed back as a string property rather than a property with a Value property. This was due to using Thoth.Json in the JavaScript and System.Text.Json in the .net version.

I added a Thoth.Json serialization encoder to the WordValues project so that the same encoder could be used by all the WordValue function/lambda implementations. 

There are two variations of the Thoth.Json package, both with the same Api. `Thoth.Json` is used for Fable projects and `Thoth.Json.Net` is for .net projects. Because the Api is the same, and they're both netstandard-targetting packings, you can add both NuGet packages, and open the correct namespace in the source file. The `FABLE_COMPILER` flag is set when compiling under `dotnet fable`.
```cmd
dotnet paket add Thoth.Json --project WordValues
dotnet paket add Thoth.Json.Net --project WordValues
```
```fsharp
namespace WordValues

open System

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

type WordValue = { Value : int; Warning : string option }
    with
        static member Encoder (v : WordValue) =
            Encode.object [
                ("Value", Encode.int v.Value)
                match v.Warning with
                | Some warn -> ("Warning", Encode.string warn)
                | None      -> ()
            ]
```
This got me the json looking the way I wanted it. I then updated the tests to match the expected output format.

Finally, I also added another suite to the Deployment.Tests project to test the deployed JavaScript function. The only problem was that the Azure functions don't automatically serve the updated code straight after it's been deployed, so the tests might report results for the previous code until the functions are restarted.