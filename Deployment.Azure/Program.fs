module Program

open System.IO

open Pulumi
open Pulumi.FSharp
open Pulumi.AzureNative

open PulumiExtras.Core
open PulumiExtras.Azure

let parentFolder = DirectoryInfo(__SOURCE_DIRECTORY__).Parent.FullName

let publishFolder =
    Path.Combine(parentFolder, "WordValues.Azure", "bin", "Release", "net5.0", "publish")

let publishJSZip =
    Path.Combine(parentFolder, "WordValues.Azure.JS", "publish.zip")

let infra () =
    let resourceGroup = Resources.ResourceGroup("functions-rg")

    let storageAccount =
        let skuArgs = Storage.Inputs.SkuArgs(Name = inputUnion2Of2 Storage.SkuName.Standard_LRS)
        Storage.StorageAccount(
            "sa",
            Storage.StorageAccountArgs(
                ResourceGroupName = io resourceGroup.Name,
                Sku               = input skuArgs,
                Kind              = inputUnion2Of2 Storage.Kind.StorageV2
            )
        )

    let storageConnection = Storage.getConnectionString storageAccount resourceGroup

    let appServicePlan =
        let skuArgs = Web.Inputs.SkuDescriptionArgs(Tier = input "Dynamic", Name = input "Y1")

        Web.AppServicePlan(
            "functions-asp",
            Web.AppServicePlanArgs(
                ResourceGroupName = io resourceGroup.Name,
                Kind              = input "FunctionApp",
                Sku               = input skuArgs
            )
        )

    let container =
        Storage.BlobContainer(
            "zips-container",
            Storage.BlobContainerArgs(
                AccountName       = io storageAccount.Name,
                PublicAccess      = input Storage.PublicAccess.None,
                ResourceGroupName = io resourceGroup.Name
            )
        )

    let appInsights =
        Insights.Component(
            "appInsights",
            Insights.ComponentArgs(
                ApplicationType = inputUnion2Of2 Insights.ApplicationType.Web,
                Kind = input "web",
                ResourceGroupName = io resourceGroup.Name
            )
        )

    let endpoint =
        let codeBlob = Storage.uploadCode "zip" publishFolder storageAccount container resourceGroup
        let appName  = Random.decorate "app"

        let siteConfig =
            Web.Inputs.SiteConfigArgs(
                AppSettings =
                    InputList.ofNamedInputValues [
                        ("APPINSIGHTS_INSTRUMENTATIONKEY",           io appInsights.InstrumentationKey)
                        ("AzureWebJobsStorage",                      io storageConnection)
                        ("FUNCTIONS_EXTENSION_VERSION",              input "~3")
                        ("FUNCTIONS_WORKER_RUNTIME",                 input "dotnet-isolated")
                        ("WEBSITE_CONTENTAZUREFILECONNECTIONSTRING", io storageConnection)
                        ("WEBSITE_CONTENTSHARE",                     io appName)
                        ("WEBSITE_RUN_FROM_PACKAGE",                 io codeBlob.SignedReadUrl)
                    ],
                NetFrameworkVersion = input "v5.0"
            )

        let appAndEndpoint = Web.createApp "app" appName siteConfig appServicePlan resourceGroup
        appAndEndpoint.Endpoint

    let jsEndpoint =
        let codeBlob = Storage.uploadCode "jszip" publishJSZip storageAccount container resourceGroup
        let appName  = Random.decorate "jsapp"

        let siteConfig =
            Web.Inputs.SiteConfigArgs(
                AppSettings =
                    InputList.ofNamedInputValues [
                        ("APPINSIGHTS_INSTRUMENTATIONKEY",           io appInsights.InstrumentationKey)
                        ("AzureWebJobsStorage",                      io storageConnection)
                        ("FUNCTIONS_EXTENSION_VERSION",              input "~3")
                        ("FUNCTIONS_WORKER_RUNTIME",                 input "node")
                        ("WEBSITE_CONTENTAZUREFILECONNECTIONSTRING", io storageConnection)
                        ("WEBSITE_CONTENTSHARE",                     io appName)
                        ("WEBSITE_RUN_FROM_PACKAGE",                 io codeBlob.SignedReadUrl)
                        ("WEBSITE_NODE_DEFAULT_VERSION",             input "~14")
                    ],
                Http20Enabled = input true,
                NodeVersion   = input "~14"
            )

        let appAndEndpoint = Web.createApp "jsapp" appName siteConfig appServicePlan resourceGroup
        appAndEndpoint.Endpoint

    dict [
        "resouceGroup",   resourceGroup.Name  :> obj
        "storageAccount", storageAccount.Name :> obj
        "endpoint",       endpoint            :> obj
        "jsEndpoint",     jsEndpoint          :> obj
    ]

[<EntryPoint>]
let main _ =
  Deployment.run infra
