module Program

open Pulumi.FSharp
open Pulumi.Aws

let infra () =
    let administrators =
        Iam.Group(
            "administrators",
            Iam.GroupArgs(
                Name = input "Administrators"
            )
        )

    let administratorsPolicy =
        Iam.GroupPolicyAttachment(
            "administratorsPolicy",
            Iam.GroupPolicyAttachmentArgs(
                Group     = io administrators.Name,
                PolicyArn = input "arn:aws:iam::aws:policy/AdministratorAccess"
            )
        )

    let admin =
        Iam.User(
            "adminUser",
            Iam.UserArgs(
                Name = input "admin"
            )
        )

    let adminGroupMemberships  =
        Iam.UserGroupMembership(
            "adminInAdministrators",
            Iam.UserGroupMembershipArgs(
                User   = io admin.Name,
                Groups = inputList [ io administrators.Name ]
            )
        )

    let devops =
        Iam.Group(
            "devops",
            Iam.GroupArgs(
                Name = input "DevOps"
            )
        )

    let devopsLambdaPolicy =
        Iam.GroupPolicy (
            "devopsLambdaPolicy",
            Iam.GroupPolicyArgs (
                Group = io devops.Id,
                Policy = input
                    """{
                        "Version": "2012-10-17",
                        "Statement": [{
                            "Effect": "Allow",
                            "Action": [
                                "lambda:GetFunctionConfiguration",
                                "lambda:UpdateFunctionConfiguration",
                                "lambda:CreateFunction",
                                "lambda:DeleteFunction",
                                "lambda:GetPolicy"
                            ],
                            "Resource": "arn:aws:lambda:*:*:*"
                        }]
                    }"""
            )
        )

    let devopsLambdaPolicy2 =
        Iam.GroupPolicy (
            "devopsLambdaPolicy2",
            Iam.GroupPolicyArgs (
                Group = io devops.Id,
                Policy = input
                    """{
                        "Version": "2012-10-17",
                        "Statement": [{
                            "Effect": "Allow",
                            "Action": [
                                "lambda:InvokeFunction",
                                "lambda:GetFunction",
                                "lambda:UpdateFunctionCode",
                                "lambda:ListVersionsByFunction",
                                "lambda:GetFunctionCodeSigningConfig",
                                "lambda:AddPermission",
                                "lambda:RemovePermission"
                            ],
                            "Resource": "arn:aws:lambda:*:*:*:*"
                        }]
                    }"""
            )
        )

    let devopsS3Policy =
        Iam.GroupPolicy (
            "devopsS3Policy",
            Iam.GroupPolicyArgs (
                Group = io devops.Id,
                Policy = input
                    """{
                        "Version": "2012-10-17",
                        "Statement": [{
                            "Effect": "Allow",
                            "Action": [
                                "s3:*"
                            ],
                            "Resource": "*"
                        }]
                    }"""
            )
        )

    let devopsIamPolicy =
        Iam.GroupPolicy (
            "devopsIamPolicy",
            Iam.GroupPolicyArgs (
                Group = io devops.Id,
                Policy = input
                    """{
                        "Version": "2012-10-17",
                        "Statement": [{
                            "Effect": "Allow",
                            "Action": [
                                "iam:ListRoles",
                                "iam:ListPolicies",
                                "iam:GetRole",
                                "iam:CreateRole",
                                "iam:AttachRolePolicy",
                                "iam:PassRole",
                                "iam:ListRolePolicies",
                                "iam:ListAttachedRolePolicies",
                                "iam:GetUser",
                                "iam:CreateServiceLinkedRole"
                            ],
                            "Resource": "arn:aws:iam::*:*"
                        }]
                    }"""
            )
        )

    let devopsGatewayPolicy =
        Iam.GroupPolicy (
            "devopsGatewayPolicy",
            Iam.GroupPolicyArgs (
                Group = io devops.Id,
                Policy = input
                    """{
                        "Version": "2012-10-17",
                        "Statement": [{
                            "Effect": "Allow",
                            "Action": [
                                "apigateway:GET",
                                "apigateway:POST",
                                "apigateway:PATCH",
                                "apigateway:PUT",
                                "apigateway:DELETE",
                                "apigateway:UpdateRestApiPolicy"
                            ],
                            "Resource": "arn:aws:apigateway:*::*"
                        }]
                    }"""
            )
        )

    let deploy =
        Iam.User(
            "deploy",
            Iam.UserArgs(
                Name = input "deploy")
        )

    let deployGroupMemberships  =
        Iam.UserGroupMembership(
            "deployInDevops",
            Iam.UserGroupMembershipArgs(
                User   = io deploy.Name,
                Groups = inputList [ io devops.Name ]
            )
        )

    let deployAccess =
        Iam.AccessKey(
            "deployKey",
            Iam.AccessKeyArgs(
                User = io deploy.Name
            )
        )

    let deploymentStateBucket =
        S3.Bucket(
            "deploymentState",
            S3.BucketArgs()
        )

    let deploymentStateAccess =
        S3.BucketPublicAccessBlock(
            "deploymentStateAccess",
            S3.BucketPublicAccessBlockArgs(
                Bucket                = io deploymentStateBucket.Id,
                BlockPublicAcls       = input true,
                BlockPublicPolicy     = input true,
                RestrictPublicBuckets = input true,
                IgnorePublicAcls      = input true
            )
        )

    let backendStateRoot =
        deploymentStateBucket.BucketName
        |> Outputs.apply (fun bn -> $"s3://{bn}")

    dict [
        "deploy.AWS_ACCESS_KEY_ID",     deployAccess.Id     :> obj
        "deploy.AWS_SECRET_ACCESS_KEY", deployAccess.Secret :> obj
        "backendStateRoot",             backendStateRoot    :> obj
    ]

[<EntryPoint>]
let main _ =
    Deployment.run infra
