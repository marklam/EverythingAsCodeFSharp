#if FAKE
#r """paket:
    source https://api.nuget.org/v3/index.json
    nuget FSharp.Core 4.7.2
    nuget Fake.Core.Target
    nuget Fake.IO.Zip
    nuget Fake.DotNet.Cli
    nuget Fake.JavaScript.Yarn
    //"""
#endif

#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet
open Fake.JavaScript

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

type ProcessHelpers =
    static member checkResult (p : ProcessResult) =
        if p.ExitCode <> 0
        then failwithf "Expected exit code 0, but was %d" p.ExitCode

    static member checkResult (p : ProcessResult<_>) =
        if p.ExitCode <> 0
        then failwithf "Expected exit code 0, but was %d" p.ExitCode

let runExe exe workingFolder arguments =
    Command.RawCommand (exe, Arguments.ofList arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingFolder
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ProcessHelpers.checkResult

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

let publishAzureJSFunc =
    Target.create "PublishAzureJSFunc" "Publish the Azure Function as Javascript" (fun _ ->
        let projectFolder = solutionFolder </> "WordValues.Azure.JS"
        let yarnParams (opt : Yarn.YarnParams) = { opt with WorkingDirectory = projectFolder }
        DotNet.exec dotNetOpt "fable" "WordValues.Azure.JS" |> ProcessHelpers.checkResult
        Yarn.install yarnParams
        Yarn.exec "build" yarnParams
        let publishZip = System.IO.Path.Combine(projectFolder, "publish.zip")
        let zipFiles =
            !! (projectFolder </> "WordValue/**/*.*")
            ++ (projectFolder </> "host.json")
        Fake.IO.Zip.createZip projectFolder publishZip "" Fake.IO.Zip.DefaultZipLevel false zipFiles
    )

let publishAwsLambda =
    Target.create "PublishAwsLambda" "Publish the Aws Lambda" (fun _ ->
        DotNet.publish publishAwsLambdaOpt "WordValues.Aws"
        let publishFolder = System.IO.Path.Combine(solutionFolder, "WordValues.Aws", "bin", "Release", "net5.0", "linux-x64", "publish")
        let publishZip    = System.IO.Path.Combine(solutionFolder, "WordValues.Aws", "bin", "Release", "net5.0", "linux-x64", "publish.zip")
        Fake.IO.Zip.createZip publishFolder publishZip "" Fake.IO.Zip.DefaultZipLevel false ( !! (publishFolder</>"**/*.*"))
    )

let publishAwsJSLambda =
    Target.create "PublishAwsJSLambda" "Publish the Aws Lambda as Javascript" (fun _ ->
        let projectFolder = solutionFolder </> "WordValues.Aws.JS"
        let yarnParams (opt : Yarn.YarnParams) = { opt with WorkingDirectory = projectFolder }
        DotNet.exec dotNetOpt "fable" "WordValues.Aws.JS" |> ProcessHelpers.checkResult
        Yarn.install yarnParams
        Yarn.exec "build" yarnParams
        let publishZip = System.IO.Path.Combine(projectFolder, "publish.zip")
        let zipFiles =
            !! (projectFolder </> "WordValue/**/*.*")
        Fake.IO.Zip.createZip (projectFolder</>"WordValue") publishZip "" Fake.IO.Zip.DefaultZipLevel false zipFiles
    )

let localTestAzureFunc =
    Target.create "LocalTestAzureFunc" "Test the Azure Function locally" (fun _ ->
        DotNet.test testOpt "WordValues.Azure.Tests"
    )

let localTestAzureJSFunc =
    Target.create "LocalTestAzureJSFunc" "Test the Azure JavaScript Function locally" (fun _ ->
        DotNet.test testOpt "WordValues.Azure.JS.Tests"
    )

let pulumiDeployAzure =
    Target.create "PulumiDeployAzure" "Deploy to Azure" (fun _ ->
        runExe
            "pulumi"
            (solutionFolder</>"Deployment.Azure")
            [ "up"; "-y"; "-s"; "dev" ]
    )

let pulumiDeployAws =
    Target.create "PulumiDeployAws" "Deploy to Aws" (fun _ ->
        runExe
            "pulumi"
            (solutionFolder</>"Deployment.Azure")
            [ "up"; "-y"; "-s"; "dev" ]
    )

let deployedTest =
    Target.create "DeployedTest" "Test the deployment" (fun _ ->
        DotNet.test testOpt "Deployment.Tests"
    )

let publishAll =
    Target.create "PublishAll" "Publish all the Functions and Lambdas" ignore

build ==> unitTests
build ==> publishAzureFunc
build ==> publishAzureJSFunc
build ==> publishAwsLambda
build ==> publishAwsJSLambda

publishAzureFunc   ==> localTestAzureFunc
publishAzureJSFunc ==> localTestAzureJSFunc

publishAzureFunc   ==> pulumiDeployAzure
publishAzureJSFunc ==> pulumiDeployAzure

publishAwsLambda   ==> pulumiDeployAws
publishAwsJSLambda ==> pulumiDeployAws

publishAzureFunc   ==> publishAll
publishAzureJSFunc ==> publishAll
publishAwsLambda   ==> publishAll
publishAwsJSLambda ==> publishAll


// Default target
Target.runOrDefault build