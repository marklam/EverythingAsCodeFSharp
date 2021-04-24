## Build script (using Fake)

Add Fake to the dotnet tools in the repo
```cmd
dootnet tool install fake-cli
```

And create a simple fake build script, `build.fsx`
```fsharp
#if FAKE
#r """paket:
    source https://api.nuget.org/v3/index.json
    nuget FSharp.Core 4.7.2
    nuget Fake.Core.Target
    //"""
#endif

#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core

Target.create "Noop" ignore

// Default target
Target.runOrDefault "Noop"
```
The `#r """paket:` opens a block of paket-syntax package references. It's not a standard F# script syntax, Fake will pass it on to Paket to load the packages that the build script used. Placing that block under the `#if FAKE` condition stops the editor from trying to parse it.

The "intellisense.fsx" file does not exist until fake is run for the first time, so there will be an 'could not find file' error reported on that line, and 'undefined symbol' errors on the following line, so we should run fake a first time.

```cmd
dotnet fake build
```

The rest of the script creates a target called "Noop" which does nothing (`ignore` will be called to do the build, which does nothing and returns 'unit'). The target "Noop" is run as the default target.

After the build script is run, there will be a `build.fsx.lock` file created which fixes the package versions used by the fake script (and should be committed to the repo) and a `.fake` folder with some build info that doesn't need to be committed).

### Adding some real build targets
I'm going to add some targets that save me having to do a sequence of operations.
* Build, then Run the Unit Tests
* `dotnet publish` the Azure function, then test it
* Push the Azure function to Azure, then test it

So to do that, I'll define some targets for the bits and some dependencies between them. One thing I'm not keen on in Fake is the use of repeated literal strings for target names, so I'm defining:
```fsharp
module Target =
    let create name description body =
        Target.description description
        Target.create name body
        name

let noop = Target.create "Noop" "Does nothing" ignore

// Default target
Target.runOrDefault noop
```
And now the magic string "Noop" only appears once, and any subesquent use is a variable and mis-types get caught by the syntax checking. Also it hides the built-it Target.create, so I can't accidentally use the original one. And also (again) it forces me to add a description for the target.

### Build and test
To build with the dotnet cli, I needed to add the package `nuget Fake.DotNet.Cli`. But fake doesn't automatically update the referenced packages while the lock file exists, so I also deleted `build.fsx.lock` and re-ran `dotnet fake build`. After re-opening the build.fsx in the editor, I got intellisense on Fake.DotNet references.

The `Fake.DotNet.Cli` package provides the `Fake.DotNet` namespace and the `DotNet.build` and `DotNet.test` methods. The first parameter to these methods allows you to customise the parameters, or leave them unchanged by passing `id`.

```fsharp
let solutionFile = "EverythingAsCodeFSharp.sln"

let build =
    Target.create "Build" "Build the solution" (fun _ ->
        DotNet.build id solutionFile
    )

// Default target
Target.runOrDefault build
```
So now `dotnet fake build` builds the solution.
```fsharp
let unitTests =
    Target.create "UnitTests" "Run the unit tests" (fun _ ->
        DotNet.test id "WordValues.Tests"
    )
```
And `dotnet fake build -t UnitTests` runs the tests.

### Dependencies
```fsharp
open Fake.Core.TargetOperators

...

build ==> unitTests
```
tells Fake that to build the `unitTests` target, we need to build the `build` target first.

### dotnet publish the Azure Function and test under func.exe
Basically just the `dotnet publish` and `dotnet test` tasks, along with the dependency.
```fsharp
build ==> publishAzureFunc ==> localTestAzureFunc
```
