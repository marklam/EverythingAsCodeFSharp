## Create an AWS Lambda

To ensure none of this is getting Azure-specific, I'll add and deploy AWS Lambda with all the same functionality and tests.

After signing up for an AWS account, install the AWS CLI - e.g. using Chocolatey from an Administrator PowerShell prompt
```pwsh
choco install awscli
```

And install the dotnet templates for AWS projects (from your non-elevated prompt)
```cmd
dotnet new -i Amazon.Lambda.Templates
```
Then create a new project with the .net 5 runtime, in a temporary folder (so the projects can be moved about to match the folder structure in the repo)
```cmd
mkdir AwsTemp
cd AwsTemp
dotnet new lambda.CustomRuntimeFunction --name WordValues.Aws --language F#
move WordValues.Aws\src\WordValues.Aws ..
move WordValues.Aws\test\WordValues.Aws.Tests ..
```
And then tweak the projects...