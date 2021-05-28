namespace WordValues.Azure.Tests

open System
open System.IO
open System.Net
open System.Diagnostics
open System.Threading
open System.Net.Http
open System.Net.Sockets
open FSharp.Quotations.Patterns

open Xunit

module Assert =
    let Fail message =
        Assert.True(false, message)
        Unchecked.defaultof<_> // Unreachable code, here to make conditional branches have matching return types

module Path =
    let replaceLast (find, replace) (path : string) =
        let components = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        let i = components |> Array.findIndexBack ((=) find)
        components.[i] <- replace
        Path.Combine(components)

type AzureFuncConnection =
    { BaseUri : Uri }

type AzureFuncInstance (folder, port, ?extraFuncExeParams) =
    let extraFuncExeParams = defaultArg extraFuncExeParams ""
    let timeoutSeconds = 30

    let canConnectToPort port =
        try
            use c = new TcpClient("127.0.0.1", port)
            true
        with | :? SocketException -> false

    let waitForPort (proc : Process) =
        let sw = Stopwatch()
        sw.Start()
        let mutable portFound = false
        while (sw.ElapsedMilliseconds < int64 (timeoutSeconds * 1000)) && (not portFound) && (not proc.HasExited) do
            Thread.Sleep(TimeSpan.FromSeconds 1.)
            portFound <- canConnectToPort port

        if proc.HasExited then
            Error $"func.exe has exited with error code %d{proc.ExitCode}"
        elif portFound then
            Ok { BaseUri = Uri($"http://localhost:%d{port}", UriKind.Absolute) }
        else
            Error $"func.exe did not open port %d{port} within %d{timeoutSeconds} seconds"

    // TODO - Capture stdout/stderr for diagnostic
    let startInfo =
        ProcessStartInfo(
            FileName = "func.exe",
            WorkingDirectory = folder,
            UseShellExecute = false,
            Arguments = $"start %s{extraFuncExeParams} --port %d{port} --timeout %d{timeoutSeconds}")

    let proc =
        if canConnectToPort port then Assert.Fail $"Port %d{port} already in use"
        Process.Start(startInfo)

    let connection = lazy (waitForPort proc)

    new (testType : Type, funcMainMethod, port) =
        let folder =
            // Assume that the function assembly has been copied to the test's build folder
            // and is of the form
            //  (absolute path to solution)/TestAssemblyName/(bin folder)/FuncAssemblyName.dll
            // but was originally
            //  (absolute path to solution)/FuncAssemblyName/(bin folder)/FuncAssemblyName.dll
            // and the calling test class is in
            //  (absolute path to solution)/TestAssemblyName/(bin folder)/TestAssemblyName.dll

            match funcMainMethod with
            | Lambda (a, Call(x,methodInfo,y)) ->
                let copiedPath   = methodInfo.DeclaringType.Assembly.Location
                let funcName     = Path.GetFileNameWithoutExtension(copiedPath)
                let testsName    = Path.GetFileNameWithoutExtension(testType.Assembly.Location)
                let originalPath = Path.replaceLast (testsName, funcName) copiedPath

                if File.GetLastWriteTime copiedPath <> File.GetLastWriteTime originalPath then
                    failwithf "%s does not have the same timestamp as %s" copiedPath originalPath

                Path.GetDirectoryName originalPath
            | _ -> invalidArg "mainMethod" "Value should be a quotation of the function assembly's main method"
        new AzureFuncInstance(folder, port, "--no-build")

    interface IDisposable with
        member _.Dispose() =
            try
                proc.Kill(entireProcessTree=true)
            with
            | :? InvalidOperationException -> ()

    member _.GetConnection() =
        match connection.Value with
        | Ok c ->
            if proc.HasExited
            then Assert.Fail $"func.exe has exited with error code %d{proc.ExitCode}"
            else c
        | Error msg ->
            Assert.Fail msg


