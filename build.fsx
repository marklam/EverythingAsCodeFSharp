#if FAKE
#r """paket:
    source https://api.nuget.org/v3/index.json
    nuget FSharp.Core 4.7.2
    nuget Fake.Core.Target
    nuget Fake.IO.Zip
    nuget Fake.DotNet.Cli
    //"""
#endif

#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet

module Target =
    let create name description body =
        Target.description description
        Target.create name body
        name

let solutionFolder = __SOURCE_DIRECTORY__
let solutionFile = "EverythingAsCodeFSharp.sln"

let dotNetOpt (opt : DotNet.Options) =
    { opt with WorkingDirectory = solutionFolder }

let publishOpt (opt : DotNet.PublishOptions) =
    opt.WithCommon dotNetOpt

let publishAwsLambdaOpt (opt : DotNet.PublishOptions) =
    { (opt |> publishOpt) with Runtime = Some "linux-x64" }

let buildOpt (opt : DotNet.BuildOptions) =
    opt.WithCommon dotNetOpt

let testOpt (opt : DotNet.TestOptions) =
    opt.WithCommon dotNetOpt

let runExe exe workingFolder arguments =
    Command.RawCommand (exe, Arguments.ofList arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingFolder
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let build =
    Target.create "Build" "Build the solution" (fun _ ->
        DotNet.build buildOpt solutionFile
    )

let unitTests =
    Target.create "UnitTests" "Run the unit tests" (fun _ ->
        DotNet.test testOpt "WordValues.Tests"
    )

let publishAzureFunc =
    Target.create "PublishAzureFunc" "Publish the Azure Function" (fun _ ->
        DotNet.publish publishOpt "WordValues.Azure"
    )

let publishAwsLambda =
    Target.create "PublishAwsLambda" "Publish the Aws Lambda" (fun _ ->
        DotNet.publish publishAwsLambdaOpt "WordValues.Aws"
        let publishFolder = System.IO.Path.Combine(solutionFolder, "WordValues.Aws", "bin", "Release", "net5.0", "linux-x64", "publish")
        let publishZip    = System.IO.Path.Combine(solutionFolder, "WordValues.Aws", "bin", "Release", "net5.0", "linux-x64", "publish.zip")
        Fake.IO.Zip.createZip publishFolder publishZip "" Fake.IO.Zip.DefaultZipLevel false ( !! (publishFolder</>"**/*.*"))
    )

let localTestAzureFunc =
    Target.create "LocalTestAzureFunc" "Test the Azure Function locally" (fun _ ->
        DotNet.test testOpt "WordValues.Azure.Tests"
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
        DotNet.test testOpt "Deployment.Tests"
    )

build ==> unitTests
build ==> publishAzureFunc ==> localTestAzureFunc
build ==> publishAwsLambda

pulumiDeploy ==> deployedTestAzureFunc

// Default target
Target.runOrDefault build