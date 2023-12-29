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

type Storage(actor: IActorRef) =
    member _.Actor = actor

    member _.Load() = async { return! actor.Ask<LoadResult>(Load) |> Async.AwaitTask }
    member _.Save tunnels = actor.Tell(Save tunnels)

let loadFile (file: FileInfo) =
    if file.Exists
    then Some(File.ReadAllText file.FullName)
    else None

type TunnelConfigStorageModel =
    { Name: string
      SshHost: string
      SshPort: uint16
      LocalPort: uint16
      RemoteHost: string
      RemotePort: uint16 }

    static member FromTunnelConfig(c: TunnelConfig) =
        { Name = c.Name
          SshHost = c.SshHost
          SshPort = c.SshPort
          LocalPort = c.LocalPort
          RemoteHost = c.RemoteHost
          RemotePort = c.RemotePort }

    static member ToTunnelConfig s =
        TunnelConfig(Name = s.Name, SshHost = s.SshHost, SshPort = s.SshPort, LocalPort = s.LocalPort,
                     RemoteHost = s.RemoteHost, RemotePort = s.RemotePort)

let deserializeTunnelConfig (s: string) :TunnelConfigStorageModel seq =
    JsonSerializer.Deserialize<TunnelConfigStorageModel seq> s

type State =
    { Store: IsolatedStorageFile }
    
    static member ``new``() = { Store = IsolatedStorageFile.GetUserStoreForApplication() }

    member my.``open`` mode :IsolatedStorageFileStream =
        my.Store.OpenFile("ssh-manager.json", mode)
        
    member my.dispose() = my.Store.Dispose()

let private load (state: State) (ctx: AkkaPackage<State>) =
    use data_file = state.``open`` FileMode.OpenOrCreate

    let iso_data =
        if data_file.Length = 0 then
            None
        else
            Some <| StreamReader(data_file).ReadToEnd()

    data_file.Close()

    let data =
        iso_data
        |> Option.bind (Option.safeCall deserializeTunnelConfig)
        |> Option.defaultValue Seq.empty
        |> Seq.map TunnelConfigStorageModel.ToTunnelConfig
        |> Seq.toArray

    ctx.Sender.Tell(LoadResult data)
    ValueSome state

let private save (state: State) (tunnels: TunnelConfig seq) =
    use data_file = state.``open`` FileMode.Create |> StreamWriter

    tunnels
    |> Seq.map TunnelConfigStorageModel.FromTunnelConfig
    |> JsonSerializer.Serialize
    |> data_file.Write
    
    ValueSome state
    
let private handler struct (state: State, package: AkkaPackage<State>) =
    match package.Message with
    | :? Load -> load state package
    | :? Save as v -> let (Save tunnels) = v in save state tunnels
    | :? ActorLifecycles as v when v = PostStop -> state.dispose(); ValueSome state
    | _ -> ValueNone
    
type FileManager() =
    inherit FsUntypedActor<State>(State.``new``(), handler)
    
[<Sealed; AbstractClass>]
type private Helper =
    static member loadContent(store: IsolatedStorageFile) :ValueOption<string> =
        use data_file = store.OpenFile("ssh-manager.json", FileMode.OpenOrCreate)

        let iso_data =
            if data_file.Length = 0
            then ValueNone
            else ValueSome <| StreamReader(data_file).ReadToEnd()

        data_file.Close()
        iso_data
    
    static member load<'T>(store :IsolatedStorageFile) :ValueOption<'T> =
        let data =
            Helper.loadContent(store)
                  .bind(ValueOption.safeCall deserializeTunnelConfig)
                  .defaultValue(Seq.empty)
            |> Seq.map TunnelConfigStorageModel.ToTunnelConfig
            |> Seq.toArray

        ValueSome state
    
type FileObjStorage() =
    let storage = IsolatedStorageFile.GetUserStoreForApplication()
    
    interface ObjStorage with
        member _.load<'T>() :Async<'T> =  Value