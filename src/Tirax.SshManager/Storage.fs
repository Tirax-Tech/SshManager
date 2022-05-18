[<Microsoft.FSharp.Core.RequireQualifiedAccess>]
module Tirax.SshManager.Storage

open System.IO
open System.Text.Json
open Akka.Actor
open RZ.FSharp.Extension
open Tirax.SshManager.ViewModels

type FileStorageManagerOption = {
    DataFile :FileInfo
}

type Load = Load
type LoadResult = LoadResult of TunnelConfig seq
type Save = Save of TunnelConfig seq

type Storage(actor :IActorRef) =
    member _.Actor = actor
    
    member _.Load() = async { return! actor.Ask<LoadResult>(Load) |> Async.AwaitTask }
    member _.Save tunnels = actor.Tell (Save tunnels)

let deserializeTunnelConfig (s :string) = JsonSerializer.Deserialize<TunnelConfig seq> s

let loadFile (file :FileInfo) =
    if file.Exists then
        let content = File.ReadAllText file.FullName
        Option.safeCall deserializeTunnelConfig content
    else
        None
        
type FileManager(option) as my =
    inherit ReceiveActor()
    
    do my.Receive<Load>(my.Load)
    do my.Receive<Save>(my.Save)
    
    member private my.Load _ :unit =
        let data = option.DataFile |> loadFile |> Option.defaultValue Seq.empty
        in my.Sender.Tell (LoadResult data)
        
    member private my.Save (Save tunnels) :unit =
        let content = JsonSerializer.Serialize<TunnelConfig seq> tunnels
        in File.WriteAllText(option.DataFile.FullName, content)