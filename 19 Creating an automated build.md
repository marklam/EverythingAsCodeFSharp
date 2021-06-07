## Creating an automated build
This is built in yaml, following the instructions at https://docs.github.com/en/actions/reference/workflow-syntax-for-github-actions

The file needs to be in .github/workflows, so create build.yaml based on [The example dotnet build](https://github.com/actions/starter-workflows/blob/main/ci/dotnet.yml)

Editing the file in Visual Studio Code will find the schema and offer intellisense etc during editing.

I changed the trigger to read
```yaml
on: push
```
And pushed to the repo to see what happened. The workflow showed up in the "Actions" tab of the repo on github, and the build failed after 18 seconds.

### Making a working build pipeline
The first error was a dumb mistake on the fake command line (I missed the 'build' part of `dotnet fake build -f build.fsx -t Build`)

(Fix, commit, push, wait for the build)

The next problem was
```
/home/runner/.nuget/packages/microsoft.net.sdk.functions/3.0.11/build/Microsoft.NET.Sdk.Functions.Build.targets(32,5): error : It was not possible to find any compatible framework version [/tmp/1qf2uc4m.onf/WorkerExtensions.csproj]
```

I suspected this was because that package targets `netcoreapp3.1`, so I added an extra build step to the yaml to install that version of the .net core sdk.