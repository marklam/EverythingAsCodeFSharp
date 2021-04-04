## Get Pulumi
### As a download
Get the installer from https://www.pulumi.com/docs/get-started/install/versions/
### Using Chocolatey from PowerShell
1. Install PowerShell from the Windows Store https://www.microsoft.com/store/productId/9MZ1SNWT0N5D

1. Start pwsh as Administrator, and install Chocolatey following the instructions at https://chocolatey.org/install

1. `choco install pulumi`

## Create the Repository
Life is too short to be poking around in web UIs trying to find all the buttons to press and set all the options the way you want them. That's why you might want to do everything in code - code it up once and apply it consistently, as often as required. 
So ...

## Making a Pulumi project to create the Repository
The Pulumi app you installed above can create a .net project in F#, and you can add NuGet packages for the cloud objects you want to create. You describe what you want deployed by creating objects in the code.

Pulumi keeps a record of the deployed cloud state (the 'stack'), in a Pulumi-hosted storage plan, or in your own cloud storage, or in the filesystem. Because this is a single-developer experiment, and I won't be merging etc, I'll use the filesystem and commit the stack state in a (private) repo.

```cmd
mkdir MkRepo
cd MkRepo
mkdir .pulumi
pulumi login file://./.pulumi
pulumi new fsharp --force
```
The `--force` parameter is needed because the MkRepo folder isn't empty, due to the '.pulumi' folder we just created. There are a few setup questions:
```
This command will walk you through creating a new Pulumi project.
Enter a value or leave blank to accept the (default), and press <ENTER>.
Press ^C at any time to quit.
project name: (MkRepo)
project description: (A minimal F# Pulumi program) 
Deploy a repository
Created project 'MkRepo'
stack name: (dev)
Created stack 'dev'
Enter your passphrase to protect config/secrets:My secure passphrase
Re-enter your passphrase to confirm:My secure passphrase

Enter your passphrase to unlock config/secrets
    (set PULUMI_CONFIG_PASSPHRASE or PULUMI_CONFIG_PASSPHRASE_FILE to remember):My secure passphrase
...
```
## Describing the repo in the Pulumi project
```cmd
dotnet add package Pulumi.Github
```
Then change the code so that the `infra` function descibes a Github repository, and puts the repo URL in the dictionary it returns.
```fsharp
module Program

open Pulumi.FSharp
open Pulumi.Github

let infra () =
    let repo =
        Repository(
            "EverythingAsCodeFSharp",
            RepositoryArgs(
                Name = input "EverythingAsCodeFSharp",
                Description = input "Generated from MkRepo",
                Visibility = input "private",
                GitignoreTemplate = input "VisualStudio"
            )
        )

    // Export outputs here
    dict [ 
        ("EverythingAsCodeFSharp.Clone", repo.HttpCloneUrl :> obj) 
    ]

[<EntryPoint>]
let main _ =
  Deployment.run infra
```
```cmd
pulumi preview
```
You will be asked for your passphrase again. To stop this happening again, save it in the `PULUMI_CONFIG_PASSPHRASE` environment variable:
```cmd
set PULUMI_CONFIG_PASSPHRASE=My secure passphrase
```
You should now see a brief description of the Github repo that will be create, but for this to work there are [confguration settings](https://Github.com/pulumi/pulumi-Github#configuration) that need to be in-place.

Getting the token is described in [Github Docs](https://docs.Github.com/en/Github/authenticating-to-Github/creating-a-personal-access-token). Create a token with 'repo' and 'workflow' permissions and copy the value of the token.
```cmd
pulumi config set Github:token <paste token value> --secret
pulumi up
```
You will be asked to confirm the deployment, then creation should go ahead and the values shown from the dictionary return value will contain the git url for the new repo:
```
Outputs:
  + EverythingAsCodeFSharp.Clone: "https://Github.com/yourGithubusername/EverythingAsCodeFSharp.git"
```
You can then clone your new repo from the command line:
```cmd
git clone https://Github.com/yourGithubusername/EverythingAsCodeFSharp.git
```
The 'MkRepo/.pulumi' folder will contain the details of the 'stack it just deployed. You'll need to preserve that - anything sensitive is encrypted, but just in case we add our own settings that we forget to mark as 'secret', use a private repo.