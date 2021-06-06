## Store the Pulumi stack info in cloud storage
Managing the stack storage in the repo was becoming a problem by this point, especially with branches and multiple local clones.

Pulumi can also store its state to Azure Blob Storage, Amazon S3 or Google Cloud Storage. Because I already had code to create Aws Infrastructure, I decided to extend the AwsConfig project to create an S3 bucket to store the state for the Deployment.* projects.

So the state for the AwsConfig would be in a repo, and the state for the Azure Functions and Aws Lambdas would be in the bucket created by the Aws Infrastructure project.
### Creating the storage
I added the Pulumi code to add a non-public bucket, and export the s3 path for pulumi login
```fsharp
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
```
And export the path for Pulumi state storage
```diff
     dict [
         "deploy.AWS_ACCESS_KEY_ID",     deployAccess.Id     :> obj
         "deploy.AWS_SECRET_ACCESS_KEY", deployAccess.Secret :> obj
+        "backendStateRoot",             backendStateRoot    :> obj
     ]
```

The after running `pulumi up`, the otputs include 
backendStateRoot: s3://deploymentstate-xxxxxxx
### Migrating the stacks into cloud storage
Now I needed to move the stacks from `Deployment.Azure\.pulumi\ ... dev` to `azure-dev` and `Deployment.Aws\.pulumi\ ... dev` to `aws-dev` in `s3://deploymentstate-xxxxxxx`

Pulumi needs to know the passphrase for the yaml files, the region for the s3 storage, and the credentials (the ones for the Aws `deployment` user)
```cmd
    set PULUMI_CONFIG_PASSPHRASE=My secure passphras
    set AWS_ACCESS_KEY_ID=...
    set AWS_SECRET_ACCESS_KEY=...
    set AWS_REGION=eu-west-2
```
The procedure for migrating a stack is in the [Pulumi States and Backends](https://www.pulumi.com/docs/intro/concepts/state/#migrating-between-backends) documentation, but one complication is that both the stacks were called 'dev' under different projects. The way round that is to import it under the original name and then rename it.
```cmd
    cd Deployment.Azure
    pulumi stack select dev
    pulumi stack export --show-secrets --file dev.json
    pulumi logout
    pulumi login s3://deploymentstate-xxxxxxx
    pulumi stack init dev
    pulumi stack import --file dev.json
    pulumi stack rename azure-dev
    rmdir /s .pulumi
    pulumi stack select azure-dev

    cd Deployment.Aws
    pulumi login file://.pulumi
    pulumi stack select dev
    pulumi stack export --show-secrets --file dev.json
    pulumi logout
    pulumi login s3://deploymentstate-xxxxxxx
    pulumi stack init dev
    pulumi stack import --file dev.json
    pulumi stack rename aws-dev
    rmdir /s .pulumi
    pulumi stack select aws-dev
```
### Checking the tests still passed
I then ran the Deployment.Tests to check that they could still read the urls etc from the Pulumi stacks. They couldn't.
```
 System.AggregateException : One or more errors occurred. (code: -1
    stdout: 
    stderr: error: failed to load checkpoint: blob (key ".pulumi/stacks/dev.json") (code=Unknown): MissingRegion: could not find region configuration
    
    )
```
The problems were:
1. `AwsPulumiStackInstance` and `AzurePulumiStackInstance` were both passing the stack name `dev` to the `PulumiStack` test helper where they now needed `aws-dev` and `azure-dev`.
2. The stacks needed to include environment variables for `AWS_REGION`, `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY` to find the backend storage.

But I don't want to have to hard-code those variables in the code, before long I would commit their real value to the repo. In fact, I'd rather not have `PULUMI_CONFIG_PASSPHRASE` in there either. But [environment variables can be placed in a `.runsettings` file](https://docs.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file) instead, which will be easier to keep out of the repo with a `.gitignore` reference.

I created a `deployment.runsettings` file in the `Deployment.Tests` folder
```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <EnvironmentVariables>
      <PULUMI_CONFIG_PASSPHRASE>My secure passphrase</PULUMI_CONFIG_PASSPHRASE>
      <AWS_REGION>eu-west-2</AWS_REGION>
      <AWS_ACCESS_KEY_ID> ... </AWS_ACCESS_KEY_ID>
      <AWS_SECRET_ACCESS_KEY> ... </AWS_SECRET_ACCESS_KEY>
    </EnvironmentVariables>
  </RunConfiguration>
</RunSettings>

```
and added a property to `Deployment.Tests.fsproj`:
```xml
<RunSettingsFilePath>$(MSBuildProjectDirectory)\deployment.runsettings</RunSettingsFilePath>`
```
### Making tests fail helpfully
With the code as it stands, running the tests (say with `dotnet test`) will just report that the `deployment.runsettings` file does not exist.

Instead, I added (`git add -f deployment.runsettings`) a file with a commented-out `EnvironmentVariables` section, and repurposed the `envVars` parameter to the `PulumiStack` test helper to be a list of environment variables that we want to check for. If they're not found then we can fail with a helpful message.

That way, running the tests in a fresh clone will report what's missing - and the either the file can be edited or the environment variables could be set insted.

