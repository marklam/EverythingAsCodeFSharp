module Program

open System.IO

open Pulumi
open Pulumi.FSharp
open Pulumi.AzureNative.Resources
open Pulumi.AzureNative.Storage
open Pulumi.AzureNative.Storage.Inputs
open Pulumi.AzureNative.Web
open Pulumi.AzureNative.Insights

open PulumiExtras.Core
open PulumiExtras.Azure

let parentFolder = DirectoryInfo(__SOURCE_DIRECTORY__).Parent.FullName

let publishFolder =
    Path.Combine(parentFolder, "WordValues.Azure", "bin", "Release", "net5.0", "publish")

let publishJSZip =
    Path.Combine(parentFolder, "WordValues.Azure.JS", "publish.zip")

let infra () =
    let resourceGroup = ResourceGroup("functions-rg")

    let storageAccount =
        let skuArgs = SkuArgs(Name = inputUnion2Of2 SkuName.Standard_LRS)
        StorageAccount(
            "sa",
            StorageAccountArgs(
                ResourceGroupName = io resourceGroup.Name,
                Sku               = input skuArgs,
                Kind              = inputUnion2Of2 Kind.StorageV2
            )
        )

    let storageConnection = getConnectionString storageAccount resourceGroup

    let appServicePlan =
        let skuArgs = Inputs.SkuDescriptionArgs(Tier = input "Dynamic", Name = input "Y1")

        AppServicePlan(
            "functions-asp",
            AppServicePlanArgs(
                ResourceGroupName = io resourceGroup.Name,
                Kind              = input "FunctionApp",
                Sku               = input skuArgs
            )
        )

    let container =
        BlobContainer(
            "zips-container",
            BlobContainerArgs(
                AccountName       = io storageAccount.Name,
                PublicAccess      = input PublicAccess.None,
                ResourceGroupName = io resourceGroup.Name
            )
        )

    let blob =
        Blob(
            "zip",
            BlobArgs(
                AccountName       = io storageAccount.Name,
                ContainerName     = io container.Name,
                ResourceGroupName = io resourceGroup.Name,
                Type              = input BlobType.Block,
                Source            = input (FileArchive publishFolder :> AssetOrArchive)
            )
        )

    let codeBlobUrl = signedBlobReadUrl blob container storageAccount resourceGroup

    let jsBlob =
        Blob(
            "jszip",
            BlobArgs(
                AccountName       = io storageAccount.Name,
                ContainerName     = io container.Name,
                ResourceGroupName = io resourceGroup.Name,
                Type              = input BlobType.Block,
                Source            = input (FileArchive publishJSZip :> AssetOrArchive)
            )
        )

    let jsCodeBlobUrl = signedBlobReadUrl jsBlob container storageAccount resourceGroup

    let appInsights =
        Component(
            "appInsights",
            ComponentArgs(
                ApplicationType = inputUnion2Of2 ApplicationType.Web,
                Kind = input "web",
                ResourceGroupName = io resourceGroup.Name
            )
        )

    let app =
        let appName = Random.decorate "app"

        let siteConfig =
            Inputs.SiteConfigArgs(
                AppSettings =
                    InputList.ofNamedInputValues [
                        ("APPINSIGHTS_INSTRUMENTATIONKEY",           io appInsights.InstrumentationKey)
                        ("AzureWebJobsStorage",                      io storageConnection)
                        ("FUNCTIONS_EXTENSION_VERSION",              input "~3")
                        ("FUNCTIONS_WORKER_RUNTIME",                 input "dotnet-isolated")
                        ("WEBSITE_CONTENTAZUREFILECONNECTIONSTRING", io storageConnection)
                        ("WEBSITE_CONTENTSHARE",                     io appName)
                        ("WEBSITE_RUN_FROM_PACKAGE",                 io codeBlobUrl)
                    ],
                NetFrameworkVersion = input "v5.0"
            )

        WebApp(
            "app",
            WebAppArgs(
                Name              = io appName,
                Kind              = input "FunctionApp",
                ResourceGroupName = io resourceGroup.Name,
                ServerFarmId      = io appServicePlan.Id,
                SiteConfig        = input siteConfig
            )
        )

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

    let endpoint   = Output.format $"https://{app.DefaultHostName}/api/WordValue" // TODO - remove hardcoded 'api' and 'WordValue'
    let jsEndpoint = Output.format $"https://{jsApp.DefaultHostName}/api/WordValue" // TODO - remove hardcoded 'api' and 'WordValue'

    dict [
        "resouceGroup",   resourceGroup.Name  :> obj
        "storageAccount", storageAccount.Name :> obj
        "endpoint",       endpoint            :> obj
        "jsEndpoint",     jsEndpoint          :> obj
    ]


[<EntryPoint>]
let main _ =
  Deployment.run infra
