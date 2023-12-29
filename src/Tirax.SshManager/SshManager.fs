module Tirax.SshManager.SshManager

open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Net
open System.Threading.Tasks
open Akka.Actor
open Akka.Configuration
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open RZ.FSharp.Akka
open Tirax.SshManager.ManagerCommands
open Tirax.SshManager.ViewModels
open Tirax.SshManager.Models

let private config = File.ReadAllText "config.hocon" |> ConfigurationFactory.ParseString
let Actor = ActorSystem.Create("SshManager", config)

type RegisterTunnel = RegisterTunnel
type UnregisterTunnel = UnregisterTunnel of name:string
type RunTunnel = RunTunnel of TunnelConfig
type StopTunnel = StopTunnel of name:string
type UpdateModel = UpdateModel
type Quit = Quit

module SshManager =
    let validatePort port = if port < 1us then None else Some port
        
    let shutdown() =
        // Somehow using async { } causes blocking on Task.Run, while using Async.AwaitTask works fine... 🤔
        task {
            do! Task.Run(fun _ -> Actor.Terminate())
            let lifetime = Application.Current.ApplicationLifetime :?> IClassicDesktopStyleApplicationLifetime
            lifetime.Shutdown()
        } |> Async.AwaitTask
    
type private ManagerInit = ManagerInit

type SshManager(storage :Storage.Storage, model :MainWindowViewModel) as my =
    inherit FsReceiveActor()
    
    do my.Self.Tell ManagerInit
    
    let runners = Dictionary<string,IActorRef>()
    
    let init _ = async {
        let! (Storage.LoadResult tunnels) = storage.Load()
        tunnels |> Seq.iter model.Tunnels.Add
    }
    
    let registerTunnel _ =
        model |> SshManager.addTunnel Debug.WriteLine model.Tunnels.Add
        storage.Save model.Tunnels
        
    let updateModel _ =
        storage.Save model.Tunnels
        
    let tryShutdownOrElse cannot_shutdown (ctx :ActorContext) =
        if runners.Count = 0 then SshManager.shutdown() else cannot_shutdown ctx
    
    let quited (TunnelRunner.Quited name) =
        Trace.Assert <| runners.Remove name
        
    let quitedToShutdown struct (ctx, quit_message) =
        quited quit_message
        let do_nothing _ = async.Return ()
        ctx |> tryShutdownOrElse do_nothing
        
    let quitState (actor :FsActorReceivable) =
        actor.FsReceiveAsync<TunnelRunner.Quited>(quitedToShutdown)
        
    let quit struct (ctx, Quit) =
        ctx |> tryShutdownOrElse (
            fun ctx ->
                ctx.Become quitState
                runners.Values |> Seq.iter (fun r -> r.Tell PoisonPill.Instance)
                async.Return ()
            )
        
    do my.Receive<UpdateUI>(fun (UpdateUI f) -> f())
    do my.FsReceiveAsync<ManagerInit>(init)
    do my.Receive<RegisterTunnel>(registerTunnel)
    do my.Receive<UnregisterTunnel>(my.UnregisterTunnel)
    do my.Receive<RunTunnel>(my.RunTunnel)
    do my.Receive<StopTunnel>(my.StopTunnel)
    do my.Receive<UpdateModel>(updateModel)
    do my.FsReceiveAsync<Quit>(quit)
    do my.Receive<TunnelRunner.Quited>(quited)
    
    member private _.RunTunnel (RunTunnel config) :unit =
        if not (runners.ContainsKey config.Name) then
            runners[config.Name] <- ActorBase.Context.ActorOf(Props.Create<TunnelRunner.Actor>(my.Self, config), WebUtility.UrlEncode config.Name)
        
    member private _.StopTunnel (StopTunnel name) :unit =
        if runners.ContainsKey name then
            runners[name].Tell PoisonPill.Instance
        
    member private _.UnregisterTunnel (UnregisterTunnel name) :unit =
        if runners.ContainsKey name then runners[name].Tell PoisonPill.Instance
        let index = model.Tunnels |> Seq.tryFindIndex (TunnelConfig.name >> (=) name)
        if index.IsSome then
            model.Tunnels.RemoveAt index.Value
            storage.Save model.Tunnels
    
let init (model :MainWindowViewModel) =
    let storage = Storage.Storage <| Actor.ActorOf(Props.Create<Storage.FileManager>(), "storage")
    Actor.ActorOf(Props.Create<SshManager>(storage, model), "ssh-manager")