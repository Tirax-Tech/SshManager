namespace Tirax.SshManager.ViewModels

open System
open ReactiveUI

type TunnelConfig() =
    inherit ReactiveObject()
    
    let mutable name      = String.Empty 
    let mutable sshHost   = String.Empty
    let mutable sshPort   = 0us         
    let mutable localPort = 0us         
    let mutable remoteHost= String.Empty
    let mutable remotePort= 0us         
    let mutable is_running = false       
    let mutable is_waiting = false
    let mutable is_editing = false
    
    member my.Name       with get() = name       and set v = my.RaiseAndSetIfChanged(&name      , v) |> ignore
    member my.SshHost    with get() = sshHost    and set v = my.RaiseAndSetIfChanged(&sshHost   , v) |> ignore
    member my.SshPort    with get() = sshPort    and set v = my.RaiseAndSetIfChanged(&sshPort   , v) |> ignore
    member my.LocalPort  with get() = localPort  and set v = my.RaiseAndSetIfChanged(&localPort , v) |> ignore
    member my.RemoteHost with get() = remoteHost and set v = my.RaiseAndSetIfChanged(&remoteHost, v) |> ignore
    member my.RemotePort with get() = remotePort and set v = my.RaiseAndSetIfChanged(&remotePort, v) |> ignore
    member my.IsRunning  with get() = is_running
                         and set v = my.RaiseAndSetIfChanged(&is_running, v) |> ignore
                                     my.RaisePropertyChanged(nameof(my.Runnable))
                                     my.RaisePropertyChanged(nameof(my.Stoppable))
    member my.IsWaiting  with get() = is_waiting
                         and set v = my.RaiseAndSetIfChanged(&is_waiting, v) |> ignore
                                     my.RaisePropertyChanged(nameof(my.Runnable))
                                     my.RaisePropertyChanged(nameof(my.Stoppable))
    member my.IsEditing  with get() = is_editing
                         and set v = my.RaiseAndSetIfChanged(&is_editing, v) |> ignore
                                     my.RaisePropertyChanged(nameof(my.Runnable))
                                     my.RaisePropertyChanged(nameof(my.Stoppable))
    
    member this.Runnable = not (is_running || is_waiting || is_editing)
    member this.Stoppable= is_running && not is_waiting && not is_editing
    
module TunnelConfig =
    let name (config :TunnelConfig) = config.Name
    let isRunning (config :TunnelConfig) = config.IsRunning