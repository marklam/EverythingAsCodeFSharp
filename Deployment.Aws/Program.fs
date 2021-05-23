module Program

open System.IO
open FSharp.Control.Tasks

open Pulumi
open Pulumi.FSharp
open Pulumi.Aws
open PulumiExtras.Core
open PulumiExtras.Aws

let publishFile =
    let parentFolder = DirectoryInfo(__SOURCE_DIRECTORY__).Parent.FullName
    Path.Combine(parentFolder, "WordValues.Aws", "bin", "Release", "net5.0", "linux-x64", "publish.zip")

let publishFileHash = File.base64SHA256 publishFile

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

    let codeBucket =
        S3.Bucket(
            "codeBucket",
            S3.BucketArgs()
        )

    let codeBlob =
        S3.BucketObject(
            "lambdaCode",
            S3.BucketObjectArgs(
                Bucket = io codeBucket.BucketName,
                Key    = input "lambdaCode.zip",
                Source = input (File.assetOrArchive publishFile)
            )
        )

    let lambda =
        Lambda.Function(
            "wordLambda",
            Lambda.FunctionArgs(
                Runtime        = inputUnion2Of2 Lambda.Runtime.Custom,
                Handler        = input "bootstrap::WordValues.Aws.Function::functionHandler", // TODO - remove name dependency
                Role           = io lambdaRole.Arn,
                S3Bucket       = io codeBlob.Bucket,
                S3Key          = io codeBlob.Key,
                SourceCodeHash = input publishFileHash
            )
        )

    let gateway =
        ApiGateway.RestApi(
            "wordGateway",
            ApiGateway.RestApiArgs(
                Name = input "WordGateway",
                Description = input "API Gateway for the WordValue function",
                Policy = input """{
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
            )
        )

    let resource =
        ApiGateway.Resource(
            "wordResource",
            ApiGateway.ResourceArgs(
                RestApi  = io gateway.Id,
                PathPart = input "{proxy+}",
                ParentId = io gateway.RootResourceId
            )
        )

    let method =
        ApiGateway.Method(
            "wordMethod",
            ApiGateway.MethodArgs(
                HttpMethod    = input "ANY",
                Authorization = input "NONE",
                RestApi       = io gateway.Id,
                ResourceId    = io resource.Id
            )
        )

    let integration =
        ApiGateway.Integration(
            "wordIntegration",
            ApiGateway.IntegrationArgs(
                HttpMethod            = input "ANY",
                IntegrationHttpMethod = input "POST",
                ResourceId            = io resource.Id,
                RestApi               = io gateway.Id,
                Type                  = input "AWS_PROXY",
                Uri                   = io lambda.InvokeArn
            ),
            CustomResourceOptions(
                DependsOn = InputList.ofSeq [ method ]
            )
        )

    let region    = Config.Region
    let accountId = Config.getAccountId ()

    let executionArn =
        (accountId, gateway.Id)
        ||> Output.map2 (fun accId gwId -> $"arn:aws:execute-api:%s{region}:%s{accId}:%s{gwId}/*/*/*")

    let permission =
        Lambda.Permission(
            "wordPermission",
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

    let deployment =
        ApiGateway.Deployment(
            "wordDeployment",
            ApiGateway.DeploymentArgs(
                Description      = input "WordValue API deployment",
                RestApi          = io gateway.Id
            ),
            CustomResourceOptions(
                DependsOn = InputList.ofSeq [ resource; method; integration ]
            )
        )

    let stage =
        ApiGateway.Stage(
            "wordStage",
            ApiGateway.StageArgs(
                Deployment = io deployment.Id,
                RestApi    = io gateway.Id,
                StageName  = input "dev"
            )
        )

    let proxyArn =
        (deployment.ExecutionArn, stage.StageName)
        ||> Output.map2 (fun execArn stageName -> $"{execArn}{stageName}/*/{{proxy+}}")

    let lambdaProxyPermission =
        Lambda.Permission(
            "wordProxyPermission",
            Lambda.PermissionArgs(
                Action    = input "lambda:InvokeFunction",
                Function  = io lambda.Arn,
                Principal = input "apigateway.amazonaws.com",
                SourceArn = io proxyArn
            )
        )

    let endpoint =
        (gateway.Id, stage.StageName)
        ||> Output.map2 (fun gwId stageName -> $"https://%s{gwId}.execute-api.%s{region}.amazonaws.com/%s{stageName}/wordvalue") // The last component is ingored

    dict [
        "sourceHash", lambda.SourceCodeHash :> obj
        "endpoint",   endpoint              :> obj
    ]


[<EntryPoint>]
let main _ =
  Deployment.run infra
