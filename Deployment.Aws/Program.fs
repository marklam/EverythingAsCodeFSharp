module Program

open System.IO
open FSharp.Control.Tasks

open Pulumi
open Pulumi.FSharp
open Pulumi.Aws
open PulumiExtras.Core
open PulumiExtras.Aws

let parentFolder = DirectoryInfo(__SOURCE_DIRECTORY__).Parent.FullName

let publishFile =
    Path.Combine(parentFolder, "WordValues.Aws", "bin", "Release", "net5.0", "linux-x64", "publish.zip")

let jsPublishFile =
    Path.Combine(parentFolder, "WordValues.Aws.JS", "publish.zip")

let infra () =
    let lambdaRole =
        Iam.Role (
            "lambdaRole",
            Iam.RoleArgs(
                AssumeRolePolicy = input
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
                            }
                        ]
                    }"""
                )
            )

    let region    = Config.Region
    let accountId = Config.getAccountId ()

    let codeBucket =
        S3.Bucket(
            "codeBucket",
            S3.BucketArgs()
        )

    let endpoint =
        let lambdaCode = S3.uploadCode "lambdaCode" codeBucket "lambdaCode.zip" publishFile

        let lambda =
            Lambda.Function(
                "wordLambda",
                Lambda.FunctionArgs(
                    Runtime        = inputUnion2Of2 Lambda.Runtime.Custom,
                    Handler        = input "bootstrap::WordValues.Aws.Function::functionHandler", // TODO - remove name dependency
                    Role           = io lambdaRole.Arn,
                    S3Bucket       = io lambdaCode.Blob.Bucket,
                    S3Key          = io lambdaCode.Blob.Key,
                    SourceCodeHash = input lambdaCode.Hash
                )
            )

        let restApi =
            ApiGateway.RestApi(
                "wordGateway",
                ApiGateway.RestApiArgs(
                    Name = input "WordGateway",
                    Description = input "API Gateway for the WordValue function",
                    Policy = input ApiGateway.defaultRestApiPolicy
                )
            )

        let stageAndDeployment =
            restApi
            |> ApiGateway.proxyResource       "wordResource"
            |> ApiGateway.anonymousAnyMethod  "wordMethod"
            |> ApiGateway.awsProxyIntegration "wordIntegration" lambda
            |> ApiGateway.deployment          "wordDeployment" "WordValue API deployment"
            |> ApiGateway.stage               "wordStage"

        let permission      = Lambda.apiPermission   "wordPermission"      region accountId restApi lambda
        let proxyPermission = Lambda.proxyPermission "wordProxyPermission" {| stageAndDeployment with Lambda = lambda |}

        let endpoint =
            (restApi.Id, stageAndDeployment.Stage.StageName)
            ||> Output.map2 (fun gwId stageName -> $"https://%s{gwId}.execute-api.%s{region}.amazonaws.com/%s{stageName}/wordvalue") // The last component is ingored

        endpoint

    let jsEndpoint =
        let lambdaCode = S3.uploadCode "jsLambdaCode" codeBucket "jsLambdaCode.zip" jsPublishFile

        let lambda =
            Lambda.Function(
                "wordJsLambda",
                Lambda.FunctionArgs(
                    Runtime        = inputUnion2Of2 Lambda.Runtime.NodeJS14dX,
                    Handler        = input "index.functionHandler",
                    Role           = io lambdaRole.Arn,
                    S3Bucket       = io lambdaCode.Blob.Bucket,
                    S3Key          = io lambdaCode.Blob.Key,
                    SourceCodeHash = input lambdaCode.Hash
                )
            )

        let restApi =
            ApiGateway.RestApi(
                "wordJsGateway",
                ApiGateway.RestApiArgs(
                    Name = input "WordJSGateway",
                    Description = input "API Gateway for the WordValue JavaScript function",
                    Policy = input ApiGateway.defaultRestApiPolicy
                )
            )

        let stageAndDeployment =
            restApi
            |> ApiGateway.proxyResource       "wordJsResource"
            |> ApiGateway.anonymousAnyMethod  "wordJsMethod"
            |> ApiGateway.awsProxyIntegration "wordJsIntegration" lambda
            |> ApiGateway.deployment          "wordJsDeployment" "WordValue JS API deployment"
            |> ApiGateway.stage               "wordJsStage"

        let proxyPermission = Lambda.proxyPermission "wordJsProxyPermission" {| stageAndDeployment with Lambda = lambda |}
        let permission      = Lambda.apiPermission   "wordJsPermission"      region accountId restApi lambda

        let endpoint =
            (restApi.Id, stageAndDeployment.Stage.StageName)
            ||> Output.map2 (fun gwId stageName -> $"https://%s{gwId}.execute-api.%s{region}.amazonaws.com/%s{stageName}/wordvalue") // The last component is ingored

        endpoint

    dict [
        "endpoint",   endpoint   :> obj
        "jsEndpoint", jsEndpoint :> obj
    ]


[<EntryPoint>]
let main _ =
  Deployment.run infra
