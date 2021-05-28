## Second tidy-up
### Make Azure projects explicit
Some of the Deployment stuff was named "Deployment" or "Deployment.Aws", so I renamed "Deployment" to "Deployment.Azure".
### Remove duplicated Pulumi helpers from Deployment projects
Moved the PulumiExtras.fs file into its own library used from the Deployment.* projects, I used;
```cmd
dotnet new classlib --language F# --framework netcoreapp3.1`
```
because the Pulumi libraries target netcoreapp3.1

Later I renamed the project as PulumiExtras.Core so it didn't start `Pulumi.` like an official lib would.
### Remove duplication from all the Deployment.Tests fixtures
I moved the tests into an abstract base class which takes an endpoint getter. The base class is abstract so that the runner doesn't try to instanatiate it as a separate fixture.
### Reduce duplication in the Deployment.Aws project
There was lots of duplication for the .net lambda vs the JS lambda, mostly because of all the setup around the Gateway. Unfortunately, each component needs references to multiple previous components.

I found a solution that kept me fairly happy (at least for now) by:
- adding functions to build each component
- each function takes an anonymous record for context
- the function returns the context with a new field added, eg 
```fsharp
let anonymousAnyMethod name (ctx : {| Resource : Resource; RestApi : RestApi |}) =
        let method =
            ApiGateway.Method(
                name,
                ApiGateway.MethodArgs(
                    HttpMethod    = input "ANY",
                    Authorization = input "NONE",
                    RestApi       = io ctx.RestApi.Id,
                    ResourceId    = io ctx.Resource.Id
                )
            )
        {| ctx with Method = method |}
```
- these functions can then be piped.
### Reduce duplication in the Deplyoyment.Azure project
- Extracted some code to upload the code to blob storage and get the url for creating the Azure Function
- Extracted some code to create the Azure Function and return its endpoint
### Reduce duplication between Azure tests and AzureJS tests
```cmd
mkdir Testing.AzureLocal
cd Testing.AzureLocal
dotnet new classlib --language F#
git mv ..\WordValues.Azure.Tests\AzureFunc.fs AzureFuncInstance.fs
del ..\WordValues.Azure.JS.Tests\AzureFunc.fs
del Library.fs
dotnet paket add FSharp.Core --project Testing.AzureLocal
dotnet paket add xunit --project Testing.AzureLocal
dotnet restore ..\EverythingAsCodeFSharp.sln
```
And then fix up the build by adding project references to replace the AzureFunc.fs file.

While testing under the Test Explorer, I found that there was a race condition where the tests would hang if two test fixtures accessed the same stack concurrently - so I added the xunit `Collection` attribute to group the Aws tests together and the Azure tests together. That way the Azure tests could run at the same time as the Aws tests, but not at the same time as other Azure tests. 

I defined some constants for the collection names, so that they couldn't be mis-spelled causing a hang due to the mismatch.
```fsharp
module TestCollections =
    let [<Literal>] AzureStack = "Azure Stack Tests"
    let [<Literal>] AwsStack   = "Aws Stack Tests"
// and used like:
[<Collection(TestCollections.AwsStack)>]
```
Also, the local tests of the WordValues endpoints in WordValues.Azure.Tests and WordValues.Azure.JS.Tests were almost identical to the ones already refactored in Deployment.Tests - so I moved TestWordValueEndpoints.fs to a new Testing.Apis project, and used that from all those tests.

Because the Testing.* projects reference xunit, the test explorer was producing warnings in the output window about a missing testhost.dll
```
Microsoft.VisualStudio.TestPlatform.ObjectModel.TestPlatformException: Unable to find C:\git\EverythingAsCodeFSharp\Testing.Apis\bin\Debug\net5.0\testhost.dll. Please publish your test project and retry.
   at Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting.DotnetTestHostManager.GetTestHostPath(String runtimeConfigDevPath, String depsFilePath, String sourceDirectory)
```
so I added package references to those projects
```cmd
dotnet paket add Microsoft.NET.Test.Sdk --project Testing.AzureLocal
dotnet paket add Microsoft.NET.Test.Sdk --project Testing.Apis
```
That resulted in the less obtrusive warning
```
No test is available in C:\git\EverythingAsCodeFSharpTidy\Testing.Apis\bin\Debug\net5.0\Testing.Apis.dll. Make sure that test discoverer & executors are registered and platform & framework version settings are appropriate and try again.
```
### Package updates
Updated the dotnet packages with
```cmd
dotnet tool list
dotnet tool update paket
dotnet tool update fake-cli
dotnet tool update fable
dotnet paket update
```



