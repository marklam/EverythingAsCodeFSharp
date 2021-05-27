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
