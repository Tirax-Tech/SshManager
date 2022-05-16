﻿module Tirax.SshManager.SshManager

open System
open System.Diagnostics
open System.IO
open Akka.Actor
open Akka.Configuration
open RZ.FSharp.Extension.Prelude
open RZ.FSharp.Extension.Option
open Tirax.SshManager.ViewModels

let private config = File.ReadAllText "config.hocon" |> ConfigurationFactory.ParseString
let Actor = ActorSystem.Create("SshManager", config)

type RegisterTunnel = RegisterTunnel

module SshManager =
    let [<Literal>] AnyPort = 0us
    
    let parseServer (s :string) =
        let parts = s.Split(':', StringSplitOptions.TrimEntries)
        match parts.Length with
        | 1 -> Some (s, AnyPort)
        | 2 -> option { let! port = parseUInt16 parts[1] in return (parts[0], port) }
        | 0 | _ -> None
        
    let validatePort port = if port < 1us then None else Some port
        
    let addTunnel fail_flow cont (model :MainWindowViewModel) =
        let ssh_server = parseServer model.NewServerWithPort
        if ssh_server.IsNone then
            fail_flow $"Invalid SSH server format: %s{model.NewServerWithPort}"
        elif model.NewLocalPort < 1us then
            fail_flow $"Invalid local port: %d{model.NewLocalPort}"
        else
            let remote_server = parseServer model.NewDestination
            if remote_server.IsNone then
                fail_flow $"Invalid destination server format: %s{model.NewDestination}"
            else
                let ssh_host, ssh_port = ssh_server.Value
                let remote_host, remote_port = remote_server.Value
                
                TunnelConfig(
                    SshHost = ssh_host,
                    SshPort = ssh_port,
                    LocalPort = model.NewLocalPort,
                    RemoteHost = remote_host,
                    RemotePort = remote_port
                ) |> cont

type SshManager(model :MainWindowViewModel) =
    inherit UntypedActor()
    
    let registerTunnel() = model |> SshManager.addTunnel Debug.WriteLine model.Tunnels.Add
    
    override my.OnReceive m =
        match m with
        | :? RegisterTunnel -> registerTunnel()
        | _ -> my.Unhandled m

let init (model :MainWindowViewModel) =
    Actor.ActorOf(Props.Create<SshManager>(model), "ssh-manager")
    
let shutdown (actor :IActorRef) = task {
    actor.Tell(PoisonPill.Instance)
    do! Actor.Terminate()
}