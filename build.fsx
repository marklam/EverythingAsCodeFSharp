#if FAKE
#r """paket:
    source https://api.nuget.org/v3/index.json
    nuget FSharp.Core 4.7.2
    nuget Fake.Core.Target
    nuget Fake.DotNet.Cli
    //"""
#endif

#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO.FileSystemOperators
open Fake.DotNet

module Target =
    let create name description body =
        Target.description description
        Target.create name body
        name

let solutionFolder = __SOURCE_DIRECTORY__
let solutionFile = "EverythingAsCodeFSharp.sln"

let runExe exe workingFolder arguments =
    Command.RawCommand (exe, Arguments.ofList arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingFolder
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let build =
    Target.create "Build" "Build the solution" (fun _ ->
        DotNet.build id solutionFile
    )

let unitTests =
    Target.create "UnitTests" "Run the unit tests" (fun _ ->
        DotNet.test id "WordValues.Tests"
    )

let publishAzureFunc =
    Target.create "PublishAzureFunc" "Publish the Azure Function" (fun _ ->
        DotNet.publish id "WordValues.Azure"
    )

let localTestAzureFunc =
    Target.create "LocalTestAzureFunc" "Test the Azure Function locally" (fun _ ->
        DotNet.test id "WordValues.Azure.Tests"
    )

let pulumiDeploy =
    Target.create "PulumiDeploy" "Test the Azure Function locally" (fun _ ->
        runExe
            "pulumi"
            (solutionFolder</>"Deployment")
            [ "up"; "-y"; "-s"; "dev" ]
    )

let deployedTestAzureFunc =
    Target.create "DeployedTestAzureFunc" "Test the Azure Function after deployment" (fun _ ->
        DotNet.test id "Deployment.Tests"
    )

build ==> unitTests
build ==> publishAzureFunc ==> localTestAzureFunc
pulumiDeploy ==> deployedTestAzureFunc

// Default target
Target.runOrDefault build