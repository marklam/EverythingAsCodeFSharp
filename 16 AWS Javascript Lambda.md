## Running the WordValue function on Aws as JavaScript
This was a repeat of `15 Azure Javascipt Function.md` but for Aws.

Fairly quickly, I found that [James Randall](https://github.com/JamesRandall) had done this already in a an article [Creating AWS Lambda with F# and Fable](https://www.jamesdrandall.com/posts/creating_aws_lambda_with_fsharp_and_fable/) and had made the JavaScript interop bits for Fable already.

But since I was on a mission of learning by doing, I set about doing it myself. I created the project the same way as `WordValues.Azure.JS` but without the Azure func, because Aws doesn't need any magic json files etc.
```cmd
mkdir WordValues.Aws.JS
cd WordValues.Aws.JS
dotnet new classlib --language F# --framework netstandard2.1
dotnet add reference ..\WordValues\WordValues.fsproj
dotnet paket add FSharp.Core --project WordValues.Aws.JS
dotnet paket add Fable.Core --project WordValues.Aws.JS
dotnet paket add Thoth.Json --project WordValues.Aws.JS
yarn add parcel-bundler --dev
```
I couldn't find any Typescript files with the ApiGatewayRequest and ApiGatewayResponse types that the .net lambda used, so I took the source for the C# files and edited them to become the F# types for Fable.

I downloaded [APIGatewayProxyRequest.cs](https://github.com/aws/aws-lambda-dotnet/blob/master/Libraries/src/Amazon.Lambda.APIGatewayEvents/APIGatewayProxyRequest.cs) and [APIGatewayProxyResponse.cs](https://github.com/aws/aws-lambda-dotnet/blob/master/Libraries/src/Amazon.Lambda.APIGatewayEvents/APIGatewayProxyResponse.cs) and committed them to git, then edited them into valid F#.

I copied the way the `IDictionary` types are implemented, from the Typescript files that ts2fable made for the Azure step, and I made the fields into `_ option` types because they're sometimes missing from the json (for example `queryParameters`).

For the response type, I created an interface so that I could use `createEmpty` and mutation to constuct them, rather than having to supply all the fields. I might revisit that at a later point because the mutation (in this and the Azure version) feels like a bit of a cop-out.
### Building the JavaScript version
I made a Fake build target like the one for Azure, and added the block for the yarn 'build' target to bundle the JavaScript.
```fsharp
let publishAwsJSLambda =
    Target.create "PublishAwsJSLambda" "Publish the Aws Lambda as Javascript" (fun _ ->
        let projectFolder = solutionFolder </> "WordValues.Aws.JS"
        DotNet.exec dotNetOpt "fable" "WordValues.Aws.JS" |> ignore
        Yarn.exec "build" (fun opt -> { opt with WorkingDirectory = projectFolder })
        let publishZip = System.IO.Path.Combine(projectFolder, "publish.zip")
        let zipFiles =
            !! (projectFolder </> "WordValue/**/*.*")
        Fake.IO.Zip.createZip (projectFolder</>"WordValue") publishZip "" Fake.IO.Zip.DefaultZipLevel false zipFiles
    )
```    
The zip file is created relative to the WordValue sub-folder, because there are no files above that folder to include (the Azure version had a json file in the parent folder).
```diff
 {
+  "scripts": {
+    "build": "parcel build Function.fs.js --out-dir WordValue --out-file index.js"
+   },
   "devDependencies": {
     "parcel-bundler": "^1.12.5"
   }
 }
```
### Publishing the JavaScript Lambda
Publishing the JavaScript version involved adding a new blob for the `publish.zip` created by the Fake build target, the lambda had to specify Node14 as the runtime, and give an entry point, but after that it was duplicating pretty much everything that the .net lambda needed. The whole script could now do with a refactor to tidy that up.

Once the JavaScript lambda was deployed, I could test it in the Aws Console webui, and I found that I'd got the handler wrong in the lambda creation. The description for the handler setting in the Aws Console linked to a page that described the format as being basically `jsfilename.entrypoint`, so I updated the value in the Pulumi script to `index.functionHandler`.
```fsharp
    let jsLambda =
        Lambda.Function(
            "wordJsLambda",
            Lambda.FunctionArgs(
                Runtime        = inputUnion2Of2 Lambda.Runtime.NodeJS14dX,
                Handler        = input "index.functionHandler",
                Role           = io lambdaRole.Arn,
                S3Bucket       = io jsCodeBlob.Bucket,
                S3Key          = io jsCodeBlob.Key,
                SourceCodeHash = input jsPublishFileHash
            )
        )
```
I also found that I needed to make the function a JavaScript promise or it wouldn't work - I found that by comparing my function declaration with James's in the repo for the article mentioned above. That required another Fable package
```cmd
dotnet paket add Fable.Promise --project WordValues.Aws.JS
```
So the declaration was then:
```fsharp
let functionHandler (request : APIGatewayProxyRequest, _) =
    promise {
        ....
        let response = createEmpty<APIGatewayProxyResponse>
        response.headers <- createEmpty<Headers>
        ....
        return response
    }
```
### Testing the deployed JavaScript lambda
This just required a new test fixture which was a copy of Aws.fs in the Deployment.Tests project, but fetching the jsEndpoint value from the Pulumi stack.

