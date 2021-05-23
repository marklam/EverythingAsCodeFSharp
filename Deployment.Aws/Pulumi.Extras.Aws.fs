module PulumiExtras.Aws

open FSharp.Control.Tasks

open Pulumi

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
