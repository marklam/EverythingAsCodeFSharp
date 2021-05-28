namespace WordValues.Azure.JS.Tests

open System
open System.IO

open Xunit

open Testing.Apis
open WordValues.Azure.Tests

module WordValuesAzureJSFunc =
    let folder = Path.Combine(DirectoryInfo(__SOURCE_DIRECTORY__).Parent.FullName, "WordValues.Azure.JS")

type WordValuesAzureFuncInstance() =
    inherit AzureFuncInstance(WordValuesAzureJSFunc.folder, 7072)

type TestAzureFun (func : WordValuesAzureFuncInstance) =
    inherit TestWordValueEndpoints(fun () -> Uri(func.GetConnection().BaseUri, "/api/WordValue"))
    interface IClassFixture<WordValuesAzureFuncInstance>
