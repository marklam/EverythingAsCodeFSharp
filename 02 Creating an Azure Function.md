## Setting up for Azure Functions
Because at the time of writing, .net 5 isn't supported in the in-process hosting for Azure Functions, I'm following the [Isolated Process Worker instructions](https://docs.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-developer-howtos?pivots=development-environment-cli&tabs=browser).

To test locally, you don't need an Azure account, but you do need the [Azure Function Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=windows%2Ccsharp%2Cbash#v2) and the [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)

## An F# project for an HttpHandler
There aren't (at the time of writing) any templates for the isolated hosting in F#, but we'll make do:
```cmd
cd EverythingAsCodeFSharp
func init WordValues.Azure --worker-runtime dotnetisolated --language F#
```
That gets us a C# project with no functions. So now to 'fix' it a bit:
```cmd
cd WordValues.Azure
rename WordValues_Azure.csproj WordValues.Azure.fsproj
del program.cs
```
Edit the NuGet package references to include the Http Function extension worker
```diff
   <ItemGroup>
     <PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" Version="1.0.1" OutputItemType="Analyzer"/>
     <PackageReference Include="Microsoft.Azure.Functions.Worker" Version="1.0.0" />
     <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http" Version="3.0.12" />
+    <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http" Version="3.0.12" />
   </ItemGroup>
```
And edit the last `ItemGroup` node in WordValues.Azure.fsproj to compensate for F# projects not automatically including the files from the source folder
```diff
   <ItemGroup>
+    <Compile Include="Function.fs" />
+    <Compile Include="Program.fs" />
-    <None Update="host.json">
+    <None Include="host.json">
       <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
     </None>
+    <None Include="local.settings.json">
-    <None Update="local.settings.json">
       <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
       <CopyToPublishDirectory>Never</CopyToPublishDirectory>
     </None>
   </ItemGroup>
```
Create Program.fs to start up the hosting
```fsharp
module WordValues.Azure.Program

open Microsoft.Extensions.Hosting

let [<EntryPoint>] main _ =
    HostBuilder()
        .ConfigureFunctionsWorkerDefaults()
        .Build()
        .Run()
    0
```
And Function.fs with a do-almost-nothing HttpHandler
```fsharp
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
```
Now if you use `func` to start the project it will build and start up a local web server to test the function (you might be asked to allow func.exe's through the local firewall)
```cmd
func start
...
Azure Functions Core Tools
Core Tools Version:       3.0.3388 Commit hash: fb42a4e0b7fdc85fbd0bcfc8d743ff7d509122ae
Function Runtime Version: 3.0.15371.0

Functions:
        WordValue: [GET] http://localhost:7071/api/WordValue
```
Opening that Url in the browser you should see the "Hello" message, and the console window should give some information about the function invocation.