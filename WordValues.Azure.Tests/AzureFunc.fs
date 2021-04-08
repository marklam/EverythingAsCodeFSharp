namespace WordValues.Azure.Tests

open System
open System.Diagnostics
open System.Threading
open System.Net.Http
open System.Net.Sockets

open Xunit

module Assert =
    let Fail message =
        Assert.True(false, message)
        Unchecked.defaultof<_> // Unreachable code, here to make conditional branches have matching return types

type AzureFuncConnection =
    { Client : HttpClient; BaseUri : Uri }
    interface IDisposable with
        member this.Dispose() = this.Client.Dispose()

type AzureFuncInstance (folder, port) =
    let timeoutSeconds = 30

    let waitForPort (proc : Process) =
        let sw = Stopwatch()
        sw.Start()
        let mutable portFound = false
        while (sw.ElapsedMilliseconds < int64 (timeoutSeconds * 1000)) && (not portFound) && (not proc.HasExited) do
            Thread.Sleep(TimeSpan.FromSeconds 1.)
            try
                use c = new TcpClient("127.0.0.1", port)
                portFound <- true
            with | :? SocketException -> ()

        if proc.HasExited then
            Error $"func.exe has exited with error code %d{proc.ExitCode}"
        elif portFound then
            Ok {
                Client = new HttpClient(Timeout = TimeSpan.FromSeconds (float timeoutSeconds))
                BaseUri = Uri($"http://localhost:%d{port}", UriKind.Absolute)
            }
        else
            Error $"func.exe did not open port %d{port} within %d{timeoutSeconds} seconds"

    // TODO - Capture stdout/stderr for diagnostic
    let startInfo =
        ProcessStartInfo(
            FileName = "func.exe",
            WorkingDirectory = folder,
            UseShellExecute = false,
            Arguments = $"start --port %d{port} --timeout %d{timeoutSeconds}")

    let proc = Process.Start(startInfo)

    let connection = lazy (waitForPort proc)

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


