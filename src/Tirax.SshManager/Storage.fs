[<Microsoft.FSharp.Core.RequireQualifiedAccess>]
module Tirax.SshManager.Storage

open System.IO
open System.IO.IsolatedStorage
open System.Text.Json
open Akka.Actor
open RZ.FSharp.Akka
open RZ.FSharp.Extension
open Tirax.SshManager.ViewModels

type Load = Load
type LoadResult = LoadResult of TunnelConfig seq
type Save = Save of TunnelConfig seq

type Storage(actor :IActorRef) =
    member _.Actor = actor
    
    member _.Load() = async { return! actor.Ask<LoadResult>(Load) |> Async.AwaitTask }
    member _.Save tunnels = actor.Tell (Save tunnels)

let loadFile (file :FileInfo) =
    if file.Exists
    then Some (File.ReadAllText file.FullName)
    else None
    
type TunnelConfigStorageModel = {
    Name :string
    SshHost :string
    SshPort :uint16
    LocalPort :uint16
    RemoteHost :string
    RemotePort :uint16
}
with
    static member FromTunnelConfig (c :TunnelConfig) =
        { Name = c.Name
          SshHost = c.SshHost
          SshPort = c.SshPort
          LocalPort = c.LocalPort
          RemoteHost = c.RemoteHost
          RemotePort = c.RemotePort }
    static member ToTunnelConfig s =
        TunnelConfig(Name = s.Name,
                     SshHost = s.SshHost,
                     SshPort = s.SshPort,
                     LocalPort = s.LocalPort,
                     RemoteHost = s.RemoteHost,
                     RemotePort = s.RemotePort)

let deserializeTunnelConfig (s :string) = JsonSerializer.Deserialize<TunnelConfigStorageModel seq> s
        
type FileManager() as my =
    inherit FsReceiveActor()
    
    let iso_store = IsolatedStorageFile.GetUserStoreForApplication()
    
    let openDataFile mode = iso_store.OpenFile("ssh-manager.json", mode)
    
    let load struct (my, Load) =
        use data_file = openDataFile FileMode.OpenOrCreate
        let iso_data = if data_file.Length = 0
                       then None
                       else Some <| StreamReader(data_file).ReadToEnd()
        data_file.Close()
        let data = iso_data |> Option.bind (Option.safeCall deserializeTunnelConfig)
                            |> Option.defaultValue Seq.empty
                            |> Seq.map TunnelConfigStorageModel.ToTunnelConfig
                            |> Seq.toArray
        my.Sender.Tell (LoadResult data)
        
    let save struct (_, Save tunnels) =
        use data_file = openDataFile FileMode.Create |> StreamWriter
        tunnels |> Seq.map TunnelConfigStorageModel.FromTunnelConfig
                |> JsonSerializer.Serialize
                |> data_file.Write
        data_file.Close()
    
    do my.FsReceive(load)
    do my.FsReceive(save)
    
    override _.PostStop () =
        iso_store.Dispose()