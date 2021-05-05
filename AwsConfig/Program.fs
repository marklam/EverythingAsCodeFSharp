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

    dict [
        "deploy.AWS_ACCESS_KEY_ID",     deployAccess.Id :> obj
        "deploy.AWS_SECRET_ACCESS_KEY", deployAccess.Secret :> obj
    ]

[<EntryPoint>]
let main _ =
    Deployment.run infra
