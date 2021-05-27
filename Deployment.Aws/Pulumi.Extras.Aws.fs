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
    let uploadCode name (bucket : S3.Bucket) blobName zipFilePath =
        let hash = File.base64SHA256 zipFilePath

        let blob =
            S3.BucketObject(
                name,
                S3.BucketObjectArgs(
                    Bucket = io bucket.BucketName,
                    Key    = input blobName,
                    Source = input (File.assetOrArchive zipFilePath)
            )
        )

        {| Hash = hash; Blob = blob |}

module ApiGateway =
    open Pulumi.Aws.ApiGateway

    let defaultRestApiPolicy =
        """{
    "Version": "2012-10-17",
    "Statement": [
    {
        "Action": "sts:AssumeRole",
        "Principal": {
        "Service": "lambda.amazonaws.com"
        },
        "Effect": "Allow",
        "Sid": ""
    },
    {
        "Action": "execute-api:Invoke",
        "Resource": "*",
        "Principal": "*",
        "Effect": "Allow",
        "Sid": ""
    }
    ]
}"""

    let proxyResource name (restApi : RestApi) =
        let resource =
            ApiGateway.Resource(
                name,
                ApiGateway.ResourceArgs(
                    RestApi  = io restApi.Id,
                    PathPart = input "{proxy+}",
                    ParentId = io restApi.RootResourceId
                )
            )
        {| Resource = resource; RestApi = restApi |}

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

    let awsProxyIntegration name (lambda : Lambda.Function) (ctx : {| Resource : Resource; RestApi : RestApi; Method : Method |}) =
        let integration =
            ApiGateway.Integration(
                name,
                ApiGateway.IntegrationArgs(
                    HttpMethod            = input "ANY",
                    IntegrationHttpMethod = input "POST",
                    ResourceId            = io ctx.Resource.Id,
                    RestApi               = io ctx.RestApi.Id,
                    Type                  = input "AWS_PROXY",
                    Uri                   = io lambda.InvokeArn
                ),
                CustomResourceOptions(
                    DependsOn = InputList.ofSeq [ ctx.Method ]
                )
            )
        {| ctx with Integration = integration |}

    let deployment name description (ctx : {| Integration : Integration; Resource : Resource; RestApi : RestApi; Method : Method |}) =
        let deployment =
            ApiGateway.Deployment(
                name,
                ApiGateway.DeploymentArgs(
                    Description      = input description,
                    RestApi          = io ctx.RestApi.Id
                ),
                CustomResourceOptions(
                    DependsOn = InputList.ofSeq [ ctx.Resource; ctx.Method; ctx.Integration ]
                )
            )
        {| RestApi = ctx.RestApi; Deployment = deployment |}

    let stage name (ctx : {| Deployment : Deployment; RestApi : RestApi |}) =
        let stage =
            ApiGateway.Stage(
                name,
                ApiGateway.StageArgs(
                    Deployment = io ctx.Deployment.Id,
                    RestApi    = io ctx.RestApi.Id,
                    StageName  = input "dev"
                )
            )
        {| Stage = stage; Deployment = ctx.Deployment |}

module Lambda =
    let apiPermission name region accountId (restApi : ApiGateway.RestApi) (lambda : Lambda.Function) =
        let executionArn =
            (accountId, restApi.Id)
            ||> Output.map2 (fun accId gwId -> $"arn:aws:execute-api:%s{region}:%s{accId}:%s{gwId}/*/*/*")

        Lambda.Permission(
            name,
            Lambda.PermissionArgs(
                Action            = input "lambda:InvokeFunction",
                Function          = io lambda.Name,
                Principal         = input "apigateway.amazonaws.com",
                SourceArn         = io executionArn,
                StatementIdPrefix = input "lambdaPermission"
            ),
            CustomResourceOptions(
                DeleteBeforeReplace = true
            )
        )

    let proxyPermission name (ctx : {| Stage : ApiGateway.Stage; Deployment : ApiGateway.Deployment; Lambda : Lambda.Function |}) =
        let proxyArn =
            (ctx.Deployment.ExecutionArn, ctx.Stage.StageName)
            ||> Output.map2 (fun execArn stageName -> $"{execArn}{stageName}/*/{{proxy+}}")

        let lambdaProxyPermission =
            Lambda.Permission(
                name,
                Lambda.PermissionArgs(
                    Action    = input "lambda:InvokeFunction",
                    Function  = io ctx.Lambda.Arn,
                    Principal = input "apigateway.amazonaws.com",
                    SourceArn = io proxyArn
                )
            )

        lambdaProxyPermission
