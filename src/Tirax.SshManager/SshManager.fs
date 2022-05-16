module Tirax.SshManager.SshManager

open System
open System.Diagnostics
open System.IO
open System.Threading.Tasks
open Akka.Actor
open Akka.Configuration
open RZ.FSharp.Extension.Prelude
open RZ.FSharp.Extension.Option
open Tirax.SshManager.ViewModels

let private config = File.ReadAllText "config.hocon" |> ConfigurationFactory.ParseString
let Actor = ActorSystem.Create("SshManager", config)

type RegisterTunnel = RegisterTunnel
type Quit = Quit

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
                
                { SshHost = ssh_host
                  SshPort = ssh_port
                  LocalPort = model.NewLocalPort
                  RemoteHost = remote_host
                  RemotePort = remote_port }
                |> cont
                
    let shutdown() :Task = Task.Run(fun _ -> Actor.Terminate())

type SshManager(model :MainWindowViewModel) as self =
    inherit ReceiveActor()
    
    let registerTunnel _ = model |> SshManager.addTunnel Debug.WriteLine model.Tunnels.Add
    let quit _ = SshManager.shutdown()
    
    do self.Receive<RegisterTunnel>(registerTunnel)
    do self.ReceiveAsync<Quit>(quit)
    
let init (model :MainWindowViewModel) =
    Actor.ActorOf(Props.Create<SshManager>(model), "ssh-manager")