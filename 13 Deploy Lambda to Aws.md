## Deploying Aws Lambda
Because I couldn't find any examples of deploying .net 5 functions to Aws Lambda using Pulumi, I decided to deploy using the Aws command-line tools and then recreate the same in Pulumi.
### Deploying with the AWS tools
The Aws Lambda project had the information on installing the command-line tools, so set the environment variables for the `deploy` account created previously, and:
```cmd
set AWS_ACCESS_KEY_ID=Access key id value
set AWS_SECRET_ACCESS_KEY=secret access key value
dotnet tool install -g Amazon.Lambda.Tools
cd WordValues.Aws
dotnet lambda deploy-function --region eu-west-2 --function-role tool-lambda-role --function-name wordvalues-aws
```
As the tool attempts to do the deployment, it reports various permissions that the `deploy` account needs but does not have, eg:
```
Error retrieving configuration for function WordValues.Aws: User: arn:aws:iam::XXXXXXXXXXXX:user/deploy is not authorized to perform: lambda:GetFunctionConfiguration on resource: arn:aws:lambda:eu-west-2:XXXXXXXXXXXX:function:WordValues.Aws
```
So add to the Aws config pulumi script:
```fsharp
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
                                "lambda:GetFunctionConfiguration"
                            ],
                            "Resource": "arn:aws:lambda:*:*:*"
                        }]
                    }"""
            )
        )
```
Then retry the deployment, find the next error and rinse and repeat.
### Reviewing the Aws-tools-deployed version
Once the deployment completes, the lamda can be tested from the Aws management page for the Lambda function, replace the sample json with a word *in quotes*.

We can look at what was created to decide what to put in the Pulumi deployment script.

We saw that it created a role, "tool-lambda-role", which we can get details of:
```cmd
aws iam get-role --role-name tool-lambda-role
```
```json
{
    "Role": {
        "Path": "/",
        "RoleName": "tool-lambda-role",
        "RoleId": "XXXXXXXXXXXXXXXXXXXXX",
        "Arn": "arn:aws:iam::XXXXXXXXXXXX:role/tool-lambda-role",
        "CreateDate": "2021-05-08T19:26:39+00:00",
        "AssumeRolePolicyDocument": {
            "Version": "2012-10-17",
            "Statement": [
                {
                    "Sid": "",
                    "Effect": "Allow",
                    "Principal": {
                        "Service": "lambda.amazonaws.com"
                    },
                    "Action": "sts:AssumeRole"
                }
            ]
        },
        "MaxSessionDuration": 3600,
        "RoleLastUsed": {}
    }
}
```
And we can get the configuration of the function, too:
```cmd
aws lambda get-function --function-name wordvalues-aws --region eu-west-2
```
```json
{
    "Configuration": {
        "FunctionName": "wordvalues-aws",
        "FunctionArn": "arn:aws:lambda:eu-west-2:XXXXXXXXXXXX:function:wordvalues-aws",
        "Runtime": "provided",
        "Role": "arn:aws:iam::XXXXXXXXXXXX:role/tool-lambda-role",
        "Handler": "bootstrap::WordValues.Aws.Function::functionHandler",
        "CodeSize": 33982022,
        "Description": "",
        "Timeout": 30,
        "MemorySize": 256,
        "LastModified": "2021-05-08T20:15:00.508+0000",
        "CodeSha256": "a9wYwrk3M2u3BNmtr9NikgWKxk5hP3Nzbn8wBck/sxc=",
        "Version": "$LATEST",
        "TracingConfig": {
            "Mode": "PassThrough"
        },
        "RevisionId": "a6eab6cd-9018-4744-9124-f556946a4176",
        "State": "Active",
        "LastUpdateStatus": "Successful",
        "PackageType": "Zip"
    },
    "Code": {
        "RepositoryType": "S3",
        "Location": "https://awslambda-eu-west-2-tasks.s3.eu-west-2.amazonaws.com/snapshots/XXXXXXXXXXXX/wordvalues-aws-XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX?versionId=..."
    }
}
```
### Deploying from Pulumi
So we can start building the Pulumi project, Deployment.Aws based on [The sample F# webserver lambda](https://github.com/pulumi/examples/blob/master/aws-fs-lambda-webserver/pulumi/Program.fs) and [A lambda function with a gateway in go](https://github.com/pulumi/examples/blob/master/aws-go-lambda-gateway/main.go), and the Azure Deployment project we've already done.

I tried to deploy just the published folder first:
```cmd
pulumi config set aws:region eu-west-2
pulumi up
```
and started getting errors about roles Pulumi needed but the Aws tool didn't, including `iam:ListRolePolicies` and  `iam:ListAttachedRolePolicies`. Once past those, I got some less useful errors:
```
    error: 1 error occurred:
        * creating urn:pulumi:dev::Deployment::aws:lambda/function:Function::wordLambda: 1 error occurred:
        * error waiting for Lambda Function (wordLambda-XXXXXXX) creation: AccessDeniedException:
        status code: 403
```        
Since there's no way to tell from that what caused the AccessDenied, I had to enable CloudTrail in the Aws management console, and then review the events (they take a few minutes to come through). Filter for user name "deploy", and hit the cog to add "Error Code" to the columns.

When I initially reviewed the logs, I downloaded the events as Json and wrote a script to filter them for AccessDenied. That is shown in the document `14 Search CloudTrail logs.md`

With all the extra roles from the CloudTrail error messages, I got the lambda itself deployed and tested it from the Test page in Aws management.
### Creating the http gateway
Copying the go sample again, I added permissions
- `aws:apigateway:RestApi`
- `aws:apigateway:Deployment`
- `aws:apigateway:Resource`
- `aws:apigateway:Method`
- `aws:lambda:Permission`
- `aws:apigateway:Integration`

and tried another deployment. This was a repeat of the loop of reviewing the CloudTrail logs and adding more required permissions to the 'deploy' account via the `AwsConfig` Pulumi project.

Another problem was that I missed the `CustomResourceOptions.DependsOn` settings when converting the Go code, so that led to a BadRequestException that isn't as obvious to fix. There was no giveaway error in the CloudTrail logs, but the problem was that the deployment and integration need to be done in a certain order.

After that, I had problem updating the permissions:
```
    error: 1 error occurred:
        * updating urn:pulumi:dev::Deployment::aws:lambda/permission:Permission::wordPermission: 1 error occurred:
        * doesn't support update
```
Changing the `CustomResourceOptions` to include `DeleteBeforeReplace` didn't change the behaviour, possibly because the permissions were deployed before that flag was set, so I re-created it by:
- commenting out the block 
- deploying, which failed to delete the permissions
- adding the permission for the `deploy` user to delete lambda permissions, by updating the AwsConfig  project
- deploying, which deleted the permissions
- uncommenting the block
- deploying again, which re-created the permissions

Next the lambda had to have the type `APIGatewayProxyRequest -> APIGatewayProxyResponse`, but the body had similar logic to the Azure function.
### Problems updating the function
While I was changing the implementation of the function, I started to notice that the lambda code was not updating. The SHA256 hash shown for the code wasn't changing when I updated the code and did a `dotnet publish ...` and `pulumi up`.

After a lot of searching, I found that the best way to ensure the update happened was to do the zipping up of the publish folder, and calculate the hash - rather that just passing the folder to Pulumi to upload.

Adding a reference to Fake.IO.Zip requires updating the nuget reference at the top of `build.fsx` 
```diff
#r """paket:
     source https://api.nuget.org/v3/index.json
     nuget FSharp.Core 4.7.2
     nuget Fake.Core.Target
+    nuget Fake.IO.Zip
     nuget Fake.DotNet.Cli
     //"""
```
and also run
```cmd 
del build.fsx.lock
dotnet fake build -f build.fsx
``` 
to update the package references, otherwise the package won't be loaded.

I added a target to the Fake script which does the dotnet publish and zips up the publish folder, made some helpers to set the working directory of the `DotNet` tasks, and added a method to the Pulumi program to calculate a new SHA256 hash and Base64-encode it. 

I also discovered (by comparing my zip with the one that the Aws tools made) that it needed to be built self-contained for linux-x64. So the final build task was:
```fsharp
let publishAwsLambda =
    Target.create "PublishAwsLambda" "Publish the Aws Lambda" (fun _ ->
        DotNet.publish publishAwsLambdaOpt "WordValues.Aws"
        let publishFolder = System.IO.Path.Combine(solutionFolder, "WordValues.Aws", "bin", "Release", "net5.0", "linux-x64", "publish")
        let publishZip    = System.IO.Path.Combine(solutionFolder, "WordValues.Aws", "bin", "Release", "net5.0", "linux-x64", "publish.zip")
        Fake.IO.Zip.createZip publishFolder publishZip "" Fake.IO.Zip.DefaultZipLevel false ( !! (publishFolder</>"**/*.*"))
    )
```
And the hash was calculated with standard .net code
```fsharp
module File =
    let base64SHA256 filePath =
        use sha256 = SHA256.Create()
        use stream = File.OpenRead filePath
        let hash   = sha256.ComputeHash stream
        Convert.ToBase64String(hash)
```
Once all that is done, the gateway can be tested by opening the gateway from the list in the AWS Gateways page, select the "ANY" node and click the lightning bolt in the "TEST" column. Choose "GET" in the Method selector, enter `word=whatever` in the Query Strings box and click "Test".

It then took quite a while to get past a problem where calling the published endpoint Url resulted in an Internal Server Error every time. It turned out that it was because I'd defined a `dev` stage, but the deployment referred to a `prod` stage.

I also created a Test suite in the Deployment.Tests project to test the deployed Aws stack.
