module PulumiExtras.Core

open System
open System.Threading.Tasks

open Pulumi
open Pulumi.FSharp
open Pulumi.Random

[<AutoOpen>]
module Union =
    let union1Of2 = Union.FromT0
    let union2Of2 = Union.FromT1

[<RequireQualifiedAccess>]
module Random =
    let decorate name =
        RandomId(name + "-id", RandomIdArgs(Prefix = input name, ByteLength = input 4)).Hex

module Output =
    let map = Outputs.apply
    let zip = Outputs.pair
    let zip3 = Outputs.pair3
    let zip4 = Outputs.pair4

    let mapAsync (f : 't -> Task<'u>) (o : Output<'t>) : Output<'u> =
        let func = Func<'t, Task<'u>> f
        o.Apply<'u>(func : Func<'t, Task<'u>>)

    let flatten (o : Output<Output<'a>>) : Output<'a> =
        o.Apply<'a>(id)

    let format = Output.Format

    let secret (s : 'a) : Output<'a> =
        Output.CreateSecret<'a>(s)

