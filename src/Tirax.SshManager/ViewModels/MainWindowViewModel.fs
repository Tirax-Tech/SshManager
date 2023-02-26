namespace Tirax.SshManager.ViewModels

open System
open System.Reactive.Linq
open System.Collections.ObjectModel
open ReactiveUI
open RZ.FSharp.Extension
open Tirax.SshManager.Models
    
module private Helper =
    let validate name server_port local_port destination :Result<TunnelConfig,string> =
        let ssh_server = ServerInputFormat.parse server_port
        if ssh_server.IsNone then
            Error $"Invalid SSH server format: %s{server_port}"
        elif local_port < 1us then
            Error $"Invalid local port: %d{local_port}"
        else
            let remote_server = ServerInputFormat.parse destination
            if remote_server.IsNone then
                Error $"Invalid destination server format: %s{destination}"
            else
                let ssh_host, ssh_port = ssh_server.Value
                let remote_host, remote_port = remote_server.Value
                
                Ok <| TunnelConfig( Name = name,
                                    SshHost = ssh_host,
                                    SshPort = ssh_port,
                                    LocalPort = local_port,
                                    RemoteHost = remote_host,
                                    RemotePort = remote_port )

type MainWindowViewModel private () as model =
    inherit ViewModelBase()
    
    let mutable addable = Unchecked.defaultof<ObservableAsPropertyHelper<bool>>
    let mutable new_tunnel = Unchecked.defaultof<ObservableAsPropertyHelper<TunnelConfig>>
    
    let mutable has_error = Unchecked.defaultof<ObservableAsPropertyHelper<bool>>
    let mutable error = Unchecked.defaultof<ObservableAsPropertyHelper<string>>
    
    let mutable newConnectionName = Unchecked.defaultof<string>
    let mutable newServerWithPort = Unchecked.defaultof<string>
    let mutable newLocalPort      = Unchecked.defaultof<uint16>
    let mutable newDestination    = Unchecked.defaultof<string>
    
    let tunnels = ObservableCollection<TunnelConfig>()
    
    let resetInput() =
        newConnectionName <- String.Empty
        newServerWithPort <- String.Empty
        newLocalPort      <- 0us
        newDestination    <- String.Empty
    
    let init() =
        resetInput()
        
        let valid_tunnel = model.WhenAnyValue((fun x -> x.NewConnectionName),
                                              (fun (x: MainWindowViewModel) -> x.NewServerWithPort),
                                              (fun x -> x.NewLocalPort),
                                              (fun x -> x.NewDestination))
                                .Select(fun struct (name,server_port,port,dest) -> Helper.validate name server_port port dest)
                                              
        addable <- valid_tunnel.Select(ResultExtension.isOk).ToProperty(model, fun m -> m.Addable)
        new_tunnel <- valid_tunnel.Where(ResultExtension.isOk)
                                  .Select(ResultExtension.unwrap)
                                  .ToProperty(model, fun m -> m.NewTunnel)
        has_error <- valid_tunnel.Select(ResultExtension.isError).ToProperty(model, fun m -> m.HasError)
        error <- valid_tunnel.Where(ResultExtension.isError)
                             .Select(ResultExtension.unwrapErr)
                             .ToProperty(model, fun m -> m.Error)

    let addTunnel = ReactiveCommand.Create(fun () -> tunnels.Add(new_tunnel.Value); resetInput())
    
    static member create() :MainWindowViewModel =
        let model = MainWindowViewModel()
        model.Init()
        model
        
    member internal _.Init() = init()
    
    member my.NewConnectionName with get() = newConnectionName and set v = my.RaiseAndSetIfChanged(&newConnectionName,v) |> ignore
    member my.NewServerWithPort with get() = newServerWithPort and set v = my.RaiseAndSetIfChanged(&newServerWithPort,v) |> ignore
    member my.NewLocalPort      with get() = newLocalPort      and set v = my.RaiseAndSetIfChanged(&newLocalPort     ,v) |> ignore
    member my.NewDestination    with get() = newDestination    and set v = my.RaiseAndSetIfChanged(&newDestination   ,v) |> ignore
    
    member _.Tunnels = tunnels
    
    member _.Addable = addable.Value
    member _.NewTunnel = new_tunnel.Value
    member _.AddTunnel = addTunnel
    
    member _.HasError = has_error.Value
    member _.Error = error.Value