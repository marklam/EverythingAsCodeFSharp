namespace WordValues.Azure.Tests

open System

open Xunit

open Testing.Apis

type WordValuesAzureFuncInstance() =
    inherit AzureFuncInstance(typeof<WordValuesAzureFuncInstance>, <@ WordValues.Azure.Program.main @>, 7071)

// TODO - category for slow tests that require func.exe
type TestAzureFun (func : WordValuesAzureFuncInstance) =
    inherit TestWordValueEndpoints(fun () -> Uri(func.GetConnection().BaseUri, "/api/WordValue"))
    interface IClassFixture<WordValuesAzureFuncInstance>
