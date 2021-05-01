## Create an AWS Lambda

To ensure none of this is getting Azure-specific, I'll add and deploy AWS Lambda with all the same functionality and tests.

After signing up for an AWS account, install the AWS CLI - e.g. using Chocolatey from an Administrator PowerShell prompt
```pwsh
choco install awscli
```

And install the dotnet templates for AWS projects (from your non-elevated prompt)
```cmd
dotnet new -i Amazon.Lambda.Templates
```
Then create a new project with the .net 5 runtime, in a temporary folder (so the projects can be moved about to match the folder structure in the repo)
```cmd
mkdir AwsTemp
cd AwsTemp
dotnet new lambda.CustomRuntimeFunction --name WordValues.Aws --language F#
move WordValues.Aws\src\WordValues.Aws ..
move WordValues.Aws\test\WordValues.Aws.Tests ..
```
### Tweaking the projects
In the WordValues.Aws.fsproj file
```diff
-  <ItemGroup>
-     <PackageReference Include="Amazon.Lambda.Core"  Version="1.2.0" />
-     <PackageReference Include="Amazon.Lambda.- RuntimeSupport" Version="1.3.0" />
-     <PackageReference Include="Amazon.Lambda.- Serialization.SystemTextJson" Version="2.1.0" />
-  </ItemGroup>
```
And then add the packages with paket
```cmd
dotnet paket add FSharp.Core --project WordValues.Aws
dotnet paket add Amazon.Lambda.Core --project WordValues.Aws
dotnet paket add Amazon.Lambda.RuntimeSupport --project WordValues.Aws
dotnet paket add Amazon.Lambda.Serialization.SystemTextJson --project WordValues.Aws
```
For WordValues.Aws.Tests.fsproj
```diff
-  <ItemGroup>
-    <PackageReference Include="Amazon.Lambda.Core" Version="1.2.0" />
-    <PackageReference Include="Amazon.Lambda.TestUtilities" Version="1.2.0" />
-    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="15.5.0" />
-    <PackageReference Include="xunit" Version="2.3.1" />
-    <PackageReference Include="xunit.runner.visualstudio" Version="2.3.1" />
-    <DotNetCliToolReference Include="dotnet-xunit" Version="2.3.1" />
-  </ItemGroup>
  <ItemGroup>
-    <ProjectReference Include="..\..\src\WordValues.Aws\WordValues.Aws.fsproj" />
+    <ProjectReference Include="..\WordValues.Aws\WordValues.Aws.fsproj" />
  </ItemGroup>
```
And fix up the package references
```cmd
dotnet paket add FSharp.Core --project WordValues.Aws.Tests
dotnet paket add Microsoft.NET.Test.Sdk --project WordValues.Aws.Tests
dotnet paket add Amazon.Lambda.Core --project WordValues.Aws.Tests
dotnet paket add Amazon.Lambda.TestUtilities --project WordValues.Aws.Tests
dotnet paket add Microsoft.NET.Test.Sdk --project WordValues.Aws.Tests
dotnet paket add xunit --project WordValues.Aws.Tests
dotnet paket add xunit.runner.visualstudio --project WordValues.Aws.Tests
dotnet paket add coverlet.collector --project WordValues.Aws.Tests
```
Now the project should build and its tests should pass.
### Making the AWS Lambda do the same thing the Azure Function does
We need to change the return type of the lambda to a APIGatewayProxyResponse, which requires another package.
```cmd
dotnet paket add Amazon.Lambda.APIGatewayEvents --project WordValues.Aws
```
Then make the Aws Lambda function similar to the Azure Function version.
```diff
-    let functionHandler (input: string) (context: ILambdaContext) =
+    let functionHandler (word : string) (context: ILambdaContext) =

-        match input with
-        | null -> String.Empty
-        | _ -> input.ToUpper()
+        if not (isNull word) then
+            let result = Calculate.wordValue word
+            let content = JsonSerializer.Serialize<_>(result, JsonSerializerOptions(IgnoreNullValues = true))
+
+            APIGatewayProxyResponse(
+                StatusCode = int HttpStatusCode.OK,
+                Body       = content,
+                Headers    = dict [ ("Content-Type", "application/json") ]
+                )
+        else
+            APIGatewayProxyResponse(
+                StatusCode = int HttpStatusCode.BadRequest,
+                Body       = "Required query parameter 'word' was missing",
+                Headers    = dict [ ("Content-Type", "text/plain;charset=utf-8") ]
+                )
```
And finally, the tests should be replaced with 'ports' of the Azure Function tests.