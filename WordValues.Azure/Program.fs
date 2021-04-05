module WordValues.Azure.Program

open Microsoft.Extensions.Hosting

let [<EntryPoint>] main _ =
    HostBuilder()
        .ConfigureFunctionsWorkerDefaults()
        .Build()
        .Run()
    0
