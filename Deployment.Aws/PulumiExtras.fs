module PulumiExtras.Core

open System
open System.Threading.Tasks

open Pulumi
open Pulumi.FSharp
open Pulumi.Random
open System.Security.Cryptography
open System.IO

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
    let map2 (f : 'a -> 'b -> 'c) (a : Output<'a>) (b : Output<'b>) =
        Outputs.pair a b
        |> Outputs.apply (fun (a,b) -> f a b)

    let zip = Outputs.pair
    let zip3 = Outputs.pair3
    let zip4 = Outputs.pair4

    let getAsync (t : Task<'u>) = Output.Create<'u> t

    let mapAsync (f : 't -> Task<'u>) (o : Output<'t>) : Output<'u> =
        let func = Func<'t, Task<'u>> f
        o.Apply<'u>(func : Func<'t, Task<'u>>)

    let flatten (o : Output<Output<'a>>) : Output<'a> =
        o.Apply<'a>(id)

    let format = Output.Format

    let secret (s : 'a) : Output<'a> =
        Output.CreateSecret<'a>(s)

module InputList =
    let ofSeq xs =
        xs |> Seq.map input |> inputList

module File =
    let base64SHA256 filePath =
        use sha256 = SHA256.Create()
        use stream = File.OpenRead filePath
        let hash   = sha256.ComputeHash stream
        Convert.ToBase64String(hash)

