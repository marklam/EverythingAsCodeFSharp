module PulumiExtras.Aws

open FSharp.Control.Tasks

open Pulumi
open Pulumi.FSharp
open Pulumi.Aws

open PulumiExtras.Core

[<RequireQualifiedAccess>]
module Config =
    let getAccountId () =
        task {
            let! identity = Pulumi.Aws.GetCallerIdentity.InvokeAsync()
            return identity.AccountId
        }
        |> Output.getAsync

[<RequireQualifiedAccess>]
module File =
    let assetOrArchive path =
        FileArchive path :> Archive :> AssetOrArchive

[<RequireQualifiedAccess>]
module S3 =
    let uploadCode (bucket : S3.Bucket) blobName zipFilePath =
        let hash = File.base64SHA256 zipFilePath

        let blob =
            S3.BucketObject(
                "lambdaCode",
                S3.BucketObjectArgs(
                    Bucket = io bucket.BucketName,
                    Key    = input blobName,
                    Source = input (File.assetOrArchive zipFilePath)
            )
        )

        {| Hash = hash; Blob = blob |}