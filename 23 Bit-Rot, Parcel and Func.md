## Bit-rot, Parcel and Func

Since the last time I worked in this repo, I'd had to reinstall my dev environment,
certainly one of the best ways to find the assumptions in the build process. 
In this case it was a dependency on a Python install.

### Parcel

The `parcel-bundler` package used some Python code, and I didn't have a Python installation on my path,
so `yarn` couldn't install the dependencies.

Version 2 of the package (now named just `parcel`) is much improved and doesn't require Python, 
so I updated the JS projects to use the new version. 

The first shortcoming was that the `--out-file` option was not supported, so I followed the advice in
[the GitHub issue](https://github.com/parcel-bundler/parcel/issues/7960) to use the `parcel-namer-rewrite` plugin.

(I also had to fiddle with the `targets` section a bit to get a js file that Azure Func liked).

### Azure `func`

There was a change to the Azure `func` local hosting which required changes in the project.

`func start` produced an error:

> Microsoft.Azure.WebJobs.Script: Referenced bundle Microsoft.Azure.Functions.ExtensionBundle of version 1.8.1 does not meet the required minimum version of 2.6.1. Update your extension bundle reference in host.json to reference 2.6.1 or later. For more information see https://aka.ms/func-min-bundle-versions.

But a bit of issue-searching in the GitHub repo [found the fix](https://github.com/Azure/Azure-Functions/issues/1987#issuecomment-952420935) for `host.json`

```diff
 {
   "version": "2.0",
   "extensionBundle": {
     "id": "Microsoft.Azure.Functions.ExtensionBundle",
-     "version": "[1.*, 2.0.0)"
+     "version": "[2.*, 3.0.0)"
   }
 }
```

### Deployment
I remembered I had to set the environment variables to find the Pulumi stacks in AWS Blob storage:
- `PULUMI_CONFIG_PASSPHRASE`
- `AWS_REGION`
- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`
And I used the fake build target to make sure everything was built & published before attempting to deploy it.

```cmd
dotnet fake build -f build.fsx -t PulumiDeployAzure
```

But it turned out that I hadn't updated the build script when I changed the stack names in 'episode' 18!

Once that was fixed, it built and deployed successfully. I waited a few minutes to make sure it had propagated in Azure, and then ran the tests:

```cmd
dotnet fake build -f build.fsx -t DeployedTest
```

Happily, all the tests passed.

### Deployment II : AWS
Because I'd changed the packaging of the AWS Javascript Lambda too, I tried a deployment to AWS.

```cmd
dotnet fake build -f build.fsx -t PulumiDeployAws
```

And... it failed, because the build script attempted to deploy to AWS from the Azure Pulumi project.
It was at this point I wondered what past-me was playing at.

Once that was fixed and the AWS deployment worked, I re-ran the deployed function tests to check everything was OK with the AWS JS Lambda.

```cmd
dotnet fake build -f build.fsx -t DeployedTest
```

Success!