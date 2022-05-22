[<Microsoft.FSharp.Core.RequireQualifiedAccess>]
module Tirax.SshManager.Storage

open System.IO
open System.IO.IsolatedStorage
open System.Text.Json
open Akka.Actor
open RZ.FSharp.Akka
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
        Some (File.ReadAllText file.FullName)
    else
        None
        
type FileManager(option) as my =
    inherit FsReceiveActor()
    
    let iso_store = IsolatedStorageFile.GetUserStoreForApplication()
    
    let openDataFile () = iso_store.OpenFile("ssh-manager.json", FileMode.OpenOrCreate)
    
    let load struct (my, Load) =
        use data_file = openDataFile()
        let iso_data = if data_file.Length = 0
                       then None
                       else Some <| StreamReader(data_file).ReadToEnd()
        let data = iso_data |> Option.orElseWith (fun _ -> option.DataFile |> loadFile)
                            |> Option.bind (Option.safeCall deserializeTunnelConfig)
                            |> Option.defaultValue Seq.empty
                            |> Seq.toArray
        let need_migration = data_file.Length = 0 && data.Length > 0
        if need_migration then my.Self.Tell (Save data)
        my.Sender.Tell (LoadResult data)
        
    let save struct (_, Save tunnels) =
        let content = JsonSerializer.Serialize<TunnelConfig seq> tunnels
        use data_file = openDataFile() |> StreamWriter
        data_file.Write(content)
        if option.DataFile.Exists then option.DataFile.Delete()
    
    do my.FsReceive(load)
    do my.FsReceive(save)
    
    override _.PostStop () =
        iso_store.Dispose()