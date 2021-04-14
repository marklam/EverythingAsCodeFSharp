module PulumiExtras.Azure

open FSharp.Control.Tasks

open Pulumi
open Pulumi.FSharp
open Pulumi.AzureNative.Resources
open Pulumi.AzureNative.Storage
open Pulumi.AzureNative.Web

open PulumiExtras.Core

[<RequireQualifiedAccess>]
module InputList =
    let ofNamedInputValues nvs : InputList<Inputs.NameValuePairArgs> =
        nvs
        |> Seq.map (fun (n, v) -> Inputs.NameValuePairArgs(Name = input n, Value = v))
        |> Array.ofSeq
        |> InputList.op_Implicit

let signedBlobReadUrl (blob : Blob) (container : BlobContainer) (account : StorageAccount) (resourceGroup : ResourceGroup) : Output<string> =
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

let getConnectionString (account : StorageAccount) (resourceGroup : ResourceGroup) : Output<string> =
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


