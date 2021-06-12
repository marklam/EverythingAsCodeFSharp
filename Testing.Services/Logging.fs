namespace Testing.Services

open System.Text.RegularExpressions
open Services

open Xunit

type TestLogger() =
    static let markerExpr = Regex("{[^}]+}")

    static member Default = TestLogger()

    interface ILogger with
        member this.Log _ event =
            let markerCount =
                markerExpr.Matches(event.Message).Count

            let paramCount =
                event.Params
                |> Array.length

            Assert.Equal(markerCount, paramCount)