## Configure the AWS account

Following the AWS IAM instructions for [creating non-root users](https://docs.aws.amazon.com/IAM/latest/UserGuide/getting-started_create-admin-group.html)
### Create a project
```cmd
mkdir AwsConfig
cd AwsConfig
mkdir .pulumi
pulumi login file://./.pulumi
pulumi new fsharp -f -n AwsConfig
pulumi config set aws:region eu-west-2
```
Edit AwsConfig.fsproj
```diff
-  <ItemGroup>
-    <PackageReference Include="Pulumi.FSharp" Version="3.*" />
-  </ItemGroup>
```
And add the project references
```cmd
dotnet paket add FSharp.Core --project AwsConfig
dotnet paket add Pulumi.FSharp --project AwsConfig
dotnet paket add Pulumi.Aws --project AwsConfig
```
### Script the admin user creation
The code to create the admin user needs:
- An 'Administrator' group to belong to
```fsharp
    let administrators =
        Iam.Group(
            "administrators",
            Iam.GroupArgs(
                Name = input "Administrators"
            )
        )
```
- Administrator access for that group (which is a built-in AWS policy)
```fsharp
    let administratorsPolicy =
        Iam.GroupPolicyAttachment(
            "administratorsPolicy",
            Iam.GroupPolicyAttachmentArgs(
                Group     = io administrators.Name,
                PolicyArn = input "arn:aws:iam::aws:policy/AdministratorAccess"
            )
        )
```
- The 'admin' user, with membership of the 'Administrators' group
```fsharp
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
```
### Deploying the first time, running as the 'root' user
Since we need to use the root user to create our non-root users, navigate to the [AWS IAM dashboard](
https://console.aws.amazon.com/iam/home) logged in as root.
- Choose "My security credentials" from the drop-down
- Expand "Access keys"
- Click "Create new access key"
- Click "Show access key"

Then store the security values in environment variables and use Pulumi to create the administrator user
```cmd
set AWS_ACCESS_KEY_ID=Access key id value
set AWS_SECRET_ACCESS_KEY=secret access key value
pulumi up
```
Once that is done, you can delete the access key from the root user.
### Adding a 'deploy' user, running as the new 'admin' user
Finally we can create a 'deploy' user which will be used from our scripts to do Pulumi deployments. 
The 'admin' user will only be used to adjust the definition of the 'deploy' user when necessary.

The 'Devops' group and 'deploy' user are the same as 'Administrators' and 'admin', but without the group policy for AdministratorAccess.

In the outputs of the Pulumi `infra` method, we'll return the key and secret to use when deploying our Aws cloud components as the 'deploy' user.

```fsharp
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
```
To create our 'deploy' user we can now use the 'admin' user we created earlier, but we need to set the environment variables
to the values for the user. From the IAM dashboard:
- expand 'Access management'
- select 'Users'
- click 'admin'
- select the 'Security credentials' tab
- create an access key as before and get the secret too.
```cmd
set AWS_ACCESS_KEY_ID=Access key id value
set AWS_SECRET_ACCESS_KEY=secret access key value
pulumi up
```
Which should create the 'deploy' user, but not show the keys for using it (because they're secrets). To see them use:
```cmd
pulumi stack --show-secrets
```
and `deploy.AWS_ACCESS_KEY_ID` and `deploy.AWS_SECRET_ACCESS_KEY` are the settings to be used in the project to deploy the function.
