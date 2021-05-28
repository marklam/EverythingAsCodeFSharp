module PulumiExtras.Azure

open FSharp.Control.Tasks

open Pulumi
open Pulumi.FSharp
open Pulumi.AzureNative

open PulumiExtras.Core

[<RequireQualifiedAccess>]
module InputList =
    open Pulumi.AzureNative.Web
    let ofNamedInputValues nvs : InputList<Inputs.NameValuePairArgs> =
        nvs
        |> Seq.map (fun (n, v) -> Inputs.NameValuePairArgs(Name = input n, Value = v))
        |> Array.ofSeq
        |> InputList.op_Implicit

[<RequireQualifiedAccess>]
module Storage =
    open Pulumi.AzureNative.Storage

    let signedBlobReadUrl (blob : Blob) (container : BlobContainer) (account : StorageAccount) (resourceGroup : Resources.ResourceGroup) : Output<string> =
        Output.zip4 (blob.Name) (container.Name) (account.Name) (resourceGroup.Name)
        |> Output.mapAsync (fun (blobName, containerName, accountName, resourceGroupName) ->
            task {
                let! blobSAS =
                    ListStorageAccountServiceSAS.InvokeAsync(
                        ListStorageAccountServiceSASArgs(
                            AccountName            = accountName,
                            Protocols              = HttpProtocol.Https,
                            SharedAccessStartTime  = "2021-01-01",
                            SharedAccessExpiryTime = "2030-01-01",
                            Resource               = union2Of2 SignedResource.C,
                            ResourceGroupName      = resourceGroupName,
                            Permissions            = union2Of2 Permissions.R,
                            CanonicalizedResource  = $"/blob/{accountName}/{containerName}",
                            ContentType            = "application/json",
                            CacheControl           = "max-age=5",
                            ContentDisposition     = "inline",
                            ContentEncoding        = "deflate"
                        )
                    )
                return Output.format $"https://{accountName}.blob.core.windows.net/{containerName}/{blobName}?{blobSAS.ServiceSasToken}"
            }
        )
        |> Output.flatten

    let getConnectionString (account : StorageAccount) (resourceGroup : Resources.ResourceGroup) : Output<string> =
        (resourceGroup.Name, account.Name)
        ||> Output.zip
        |> Output.mapAsync (fun (rgName, saName) ->
            task {
                let! storageAccountKeys =
                    ListStorageAccountKeys.InvokeAsync(
                        ListStorageAccountKeysArgs(
                            ResourceGroupName = rgName,
                            AccountName = saName
                        )
                    )
                let primaryStorageKey = storageAccountKeys.Keys.[0].Value |> Output.secret

                return Output.format $"DefaultEndpointsProtocol=https;AccountName={account.Name};AccountKey={primaryStorageKey}"
            }
        )
        |> Output.flatten

    let uploadCode name filesystemPath (account : StorageAccount) (container : BlobContainer) (resourceGroup : Resources.ResourceGroup) =
        let blob =
            Storage.Blob(
                name,
                Storage.BlobArgs(
                    AccountName       = io account.Name,
                    ContainerName     = io container.Name,
                    ResourceGroupName = io resourceGroup.Name,
                    Type              = input Storage.BlobType.Block,
                    Source            = input (FileArchive filesystemPath :> AssetOrArchive)
                )
            )

        let codeBlobUrl = signedBlobReadUrl blob container account resourceGroup

        {| Blob = blob; SignedReadUrl = codeBlobUrl |}

module Web =
    let createApp name appName siteConfig (appServicePlan : Web.AppServicePlan) (resourceGroup : Resources.ResourceGroup) =
        let app =
            Web.WebApp(
                name,
                Web.WebAppArgs(
                    Name              = io appName,
                    Kind              = input "FunctionApp",
                    ResourceGroupName = io resourceGroup.Name,
                    ServerFarmId      = io appServicePlan.Id,
                    SiteConfig        = input siteConfig
                )
            )

        let endpoint =
            Output.format $"https://{app.DefaultHostName}/api/WordValue" // TODO - remove hardcoded 'api' and 'WordValue'

        {| App = app; Endpoint = endpoint |}
