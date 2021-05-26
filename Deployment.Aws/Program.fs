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

let publishFileHash   = File.base64SHA256 publishFile
let jsPublishFileHash = File.base64SHA256 jsPublishFile

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

    let jsCodeBlob =
        S3.BucketObject(
            "jsLambdaCode",
            S3.BucketObjectArgs(
                Bucket = io codeBucket.BucketName,
                Key    = input "jsLambdaCode.zip",
                Source = input (File.assetOrArchive jsPublishFile)
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

    let jsLambda =
        Lambda.Function(
            "wordJsLambda",
            Lambda.FunctionArgs(
                Runtime        = inputUnion2Of2 Lambda.Runtime.NodeJS14dX,
                Handler        = input "index.functionHandler",
                Role           = io lambdaRole.Arn,
                S3Bucket       = io jsCodeBlob.Bucket,
                S3Key          = io jsCodeBlob.Key,
                SourceCodeHash = input jsPublishFileHash
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

    let jsGateway =
        ApiGateway.RestApi(
            "wordJsGateway",
            ApiGateway.RestApiArgs(
                Name = input "WordJSGateway",
                Description = input "API Gateway for the WordValue JavaScript function",
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

    let jsResource =
        ApiGateway.Resource(
            "wordJsResource",
            ApiGateway.ResourceArgs(
                RestApi  = io jsGateway.Id,
                PathPart = input "{proxy+}",
                ParentId = io jsGateway.RootResourceId
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

    let jsMethod =
        ApiGateway.Method(
            "wordJsMethod",
            ApiGateway.MethodArgs(
                HttpMethod    = input "ANY",
                Authorization = input "NONE",
                RestApi       = io jsGateway.Id,
                ResourceId    = io jsResource.Id
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

    let jsIntegration =
        ApiGateway.Integration(
            "wordJsIntegration",
            ApiGateway.IntegrationArgs(
                HttpMethod            = input "ANY",
                IntegrationHttpMethod = input "POST",
                ResourceId            = io jsResource.Id,
                RestApi               = io jsGateway.Id,
                Type                  = input "AWS_PROXY",
                Uri                   = io jsLambda.InvokeArn
            ),
            CustomResourceOptions(
                DependsOn = InputList.ofSeq [ jsMethod ]
            )
        )

    let region    = Config.Region
    let accountId = Config.getAccountId ()

    let executionArn =
        (accountId, gateway.Id)
        ||> Output.map2 (fun accId gwId -> $"arn:aws:execute-api:%s{region}:%s{accId}:%s{gwId}/*/*/*")

    let jsExecutionArn =
        (accountId, jsGateway.Id)
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

    let jsPermission =
        Lambda.Permission(
            "wordJsPermission",
            Lambda.PermissionArgs(
                Action            = input "lambda:InvokeFunction",
                Function          = io jsLambda.Name,
                Principal         = input "apigateway.amazonaws.com",
                SourceArn         = io jsExecutionArn,
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

    let jsDeployment =
        ApiGateway.Deployment(
            "wordJsDeployment",
            ApiGateway.DeploymentArgs(
                Description      = input "WordValue JS API deployment",
                RestApi          = io jsGateway.Id
            ),
            CustomResourceOptions(
                DependsOn = InputList.ofSeq [ jsResource; jsMethod; jsIntegration ]
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

    let jsStage =
        ApiGateway.Stage(
            "wordJsStage",
            ApiGateway.StageArgs(
                Deployment = io jsDeployment.Id,
                RestApi    = io jsGateway.Id,
                StageName  = input "dev"
            )
        )

    let proxyArn =
        (deployment.ExecutionArn, stage.StageName)
        ||> Output.map2 (fun execArn stageName -> $"{execArn}{stageName}/*/{{proxy+}}")

    let jsProxyArn =
        (jsDeployment.ExecutionArn, jsStage.StageName)
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

    let jsLambdaProxyPermission =
        Lambda.Permission(
            "wordJsProxyPermission",
            Lambda.PermissionArgs(
                Action    = input "lambda:InvokeFunction",
                Function  = io jsLambda.Arn,
                Principal = input "apigateway.amazonaws.com",
                SourceArn = io jsProxyArn
            )
        )

    let endpoint =
        (gateway.Id, stage.StageName)
        ||> Output.map2 (fun gwId stageName -> $"https://%s{gwId}.execute-api.%s{region}.amazonaws.com/%s{stageName}/wordvalue") // The last component is ingored

    let jsEndpoint =
        (jsGateway.Id, jsStage.StageName)
        ||> Output.map2 (fun gwId stageName -> $"https://%s{gwId}.execute-api.%s{region}.amazonaws.com/%s{stageName}/wordvalue") // The last component is ingored

    dict [
        "sourceHash", lambda.SourceCodeHash :> obj
        "endpoint",   endpoint              :> obj
        "jsEndpoint", jsEndpoint            :> obj
    ]


[<EntryPoint>]
let main _ =
  Deployment.run infra
