## Deploying to Azure

### Set up the Azure access
First create an Azure account, you can get free tier services, free trials and free credit (for example when signing up, and through Visual Studio Dev Essentials).

Install the Azure CLI, either via the [installer](https://github.com/Azure/azure-cli/releases) or Chocolatey (from and Administrator PowerShell prompt):
```powershell
choco install azure-cli
```

Then (after re-opening your command prompt) you should be able to use the Azure CLI
```cmd
az login
```
which will open a browser window to login.

### Create the Pulumi project to deploy to the cloud
From the command prompt in the repository
```cmd
mkdir Deployment
cd Deployment
```
Optionally, if you're storing data in your (private) repo instead
```cmd
mkdir .pulumi
pulumi login file://./.pulumi
```
And then create the project
```cmd
pulumi new azure-fsharp --force
```
But the default project template doesn't use paket, so edit the fsproj
```diff
-  <ItemGroup>
-    <PackageReference Include="Pulumi.AzureNative" Version="0.*" />
-    <PackageReference Include="Pulumi.FSharp" Version="2.*" />
-  </ItemGroup>
```
and add the packages
```cmd
dotnet paket add Pulumi.AzureNative --project Deployment
dotnet paket add Pulumi.FSharp --project Deployment
```
By the time the deployment was coded up, there wasn't much left of the templated code. It was built from:
* [Pulumi example code for a C# Azure function](https://github.com/pulumi/examples/tree/master/azure-cs-functions)
* The [Azure function isolated hosting guide](https://docs.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-developer-howtos?pivots=development-environment-cli&tabs=browser)

By performing the command-line deployment explained in the Azure guide, you can find the correct settings for the Pulumi objects.
For example:
```cmd
az functionapp create --resource-group functions1234 --consumption-plan-location westeurope --runtime dotnet-isolated --runtime-version 5.0 --functions-version 3 --name wvazureclie --storage-account sa8c6d7
```
Then retrieve the settings from Pulumi code.
```fsharp
    let fn = WebApp.Get("wvazurecli", input "/subscriptions/...../resourceGroups/functions1234/providers/Microsoft.Web/sites/wvazureclie")
    let webAppKind           = fn.Kind
    let webAppConfigSettings = fn.SiteConfig |> Outputs.apply (fun c -> c.AppSettings |> Seq.map (fun o -> $"{o.Name}={o.Value}") |> String.concat ",")
    let webAppConfigNetVer   = fn.SiteConfig |> Outputs.apply (fun c -> c.NetFrameworkVersion)
    let serverFarmId         = fn.ServerFarmId
```
and return the values to be shown in the preview of `pulumi up`
```fsharp
    dict [
        "fn.Kind",                   webAppKind           :> obj
        "fn.SiteConfig.AppSettings", webAppConfigSettings :> obj
        "fn.SiteConfig.NetVer",      webAppConfigNetVer   :> obj
        "fn.ServerFarmId",           serverFarmId         :> obj
    ]
```
You can also diff the json produced by the Export tab of the Azure Web Portal for the `az func`-deployed and Pulumi-deployed versions to check that 
everything is there.

### The finished deployment code
There are a couple of rough edges, particularly hardcoding part of the function URL for the stack output
dictionary at the end. But opening the URL in the web browser shows the "Hello" message from the function 
running in 'the cloud'.