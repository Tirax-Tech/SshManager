module Tirax.SshManager.SshManager

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Threading.Tasks
open Akka.Actor
open Akka.Configuration
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Tirax.SshManager.ViewModels
open Tirax.SshManager.Models

let private config = File.ReadAllText "config.hocon" |> ConfigurationFactory.ParseString
let Actor = ActorSystem.Create("SshManager", config)

type RegisterTunnel = RegisterTunnel
type RunTunnel = RunTunnel of TunnelConfig
type Quit = Quit

module SshManager =
    let validatePort port = if port < 1us then None else Some port
        
    let addTunnel fail_flow cont (model :MainWindowViewModel) =
        let ssh_server = ServerInputFormat.parse model.NewServerWithPort
        if ssh_server.IsNone then
            fail_flow $"Invalid SSH server format: %s{model.NewServerWithPort}"
        elif model.NewLocalPort < 1us then
            fail_flow $"Invalid local port: %d{model.NewLocalPort}"
        else
            let remote_server = ServerInputFormat.parse model.NewDestination
            if remote_server.IsNone then
                fail_flow $"Invalid destination server format: %s{model.NewDestination}"
            else
                let ssh_host, ssh_port = ssh_server.Value
                let remote_host, remote_port = remote_server.Value
                
                TunnelConfig( Name = model.NewServerWithPort,
                              SshHost = ssh_host,
                              SshPort = ssh_port,
                              LocalPort = model.NewLocalPort,
                              RemoteHost = remote_host,
                              RemotePort = remote_port )
                |> cont
                
    let shutdown() :Task = task {
        do! Task.Run(fun _ -> Actor.Terminate())
        let lifetime = Application.Current.ApplicationLifetime :?> IClassicDesktopStyleApplicationLifetime
        lifetime.Shutdown()
    }
    
type private ManagerInit = ManagerInit

type SshManager(storage :Storage.Storage, model :MainWindowViewModel) as my =
    inherit ReceiveActor()
    
    do my.Self.Tell ManagerInit
    
    let runners = Dictionary<string,IActorRef>()
    
    let registerTunnel _ =
        model |> SshManager.addTunnel Debug.WriteLine model.Tunnels.Add
        storage.Save model.Tunnels
    
    let quit _ =
        if runners.Count = 0 then
            SshManager.shutdown()
        else
            model.Tunnels |> Seq.iter(fun t -> t.IsWaiting <- true)
            runners.Values |> Seq.iter (fun r -> r.Tell PoisonPill.Instance)
            Task.CompletedTask
        
    let quited (TunnelRunner.Quited name) =
        Trace.Assert <| runners.Remove name
        if runners.Count = 0
        then SshManager.shutdown()
        else Task.CompletedTask
        
    let runFailed (TunnelRunner.RunFailure (tunnel, ex)) =
        Trace.Assert <| runners.Remove tunnel.Name
        Trace.WriteLine $"Failed to run tunnel: %s{tunnel.Name} - %s{ex}"
        tunnel.IsWaiting <- false
        
    let runOk (TunnelRunner.RunOk tunnel) =
        tunnel.IsWaiting <- false
        tunnel.IsRunning <- true
    
    do my.ReceiveAsync<ManagerInit>(Func<_,_>(my.Init))
    do my.Receive<RegisterTunnel>(registerTunnel)
    do my.Receive<RunTunnel>(my.RunTunnel)
    do my.ReceiveAsync<Quit>(quit)
    do my.ReceiveAsync<TunnelRunner.Quited>(quited)
    do my.Receive<TunnelRunner.RunFailure>(runFailed)
    do my.Receive<TunnelRunner.RunOk>(runOk)
    
    member private _.Init _ =
        upcast (async {
            let! (Storage.LoadResult tunnels) = storage.Load()
            tunnels |> Seq.iter model.Tunnels.Add
        } |> Async.StartImmediateAsTask)
    
    member private _.RunTunnel (RunTunnel config) :unit =
        Trace.Assert <| not (runners.ContainsKey config.Name)
        let runner = ActorBase.Context.ActorOf(Props.Create<TunnelRunner.Actor>(my.Self, config))
        runners[config.Name] <- runner
        config.IsWaiting <- true
    
let init (model :MainWindowViewModel) =
    let data_file = FileInfo("ssh-manager.json")
    let option = { Storage.DataFile = data_file }
    let storage = Storage.Storage <| Actor.ActorOf(Props.Create<Storage.FileManager>(option), "storage")
    Actor.ActorOf(Props.Create<SshManager>(storage, model), "ssh-manager")