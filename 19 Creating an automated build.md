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

That was enough to get it building, so I changed the triggers in the yaml file to build on Pull Request or on a Push to the `main` branch.

### The default branch
The example syntax for building the default branch and Pull Requests
```yaml
on:
  push:
    branches: [ $default-branch ]
  pull_request:
    branches: [ $default-branch ]
```
didn't seem to work, but it looked likely to be due to the `default-branch` macro not having been set. 

I found that Pulumi could set this with the `DefaultBranch` property of `RepositoryArgs`, but that was marked as obsolete. The preferred approach is to use a `BranchDefault` resource:
```fsharp
    let defaultBranch =
        Pulumi.Github.BranchDefault(
            "EverythingAsCodeFSharpDefaultBranch",
            BranchDefaultArgs(
                Repository = io repo.Name,
                Branch     = input "main"
            )
        )
```

However, that still didn't work. A [StackOverflow answer](https://stackoverflow.com/a/65723433/59371) explains that the `[ $default-branch ]` is for workflow *templates*, not the workflows themselves. The macro would be replaced by `main` for a workflow yaml file.