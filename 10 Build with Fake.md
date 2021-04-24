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



