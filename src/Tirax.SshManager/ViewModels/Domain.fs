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
    let mutable isRunning = false       
    let mutable isWaiting = false       
    
    member my.Name       with get() = name       and set v = my.RaiseAndSetIfChanged(&name      , v) |> ignore
    member my.SshHost    with get() = sshHost    and set v = my.RaiseAndSetIfChanged(&sshHost   , v) |> ignore
    member my.SshPort    with get() = sshPort    and set v = my.RaiseAndSetIfChanged(&sshPort   , v) |> ignore
    member my.LocalPort  with get() = localPort  and set v = my.RaiseAndSetIfChanged(&localPort , v) |> ignore
    member my.RemoteHost with get() = remoteHost and set v = my.RaiseAndSetIfChanged(&remoteHost, v) |> ignore
    member my.RemotePort with get() = remotePort and set v = my.RaiseAndSetIfChanged(&remotePort, v) |> ignore
    member my.IsRunning  with get() = isRunning
                         and set v = my.RaiseAndSetIfChanged(&isRunning , v) |> ignore
                                     my.RaisePropertyChanged(nameof(my.Runnable))
                                     my.RaisePropertyChanged(nameof(my.Stoppable))
    member my.IsWaiting  with get() = isWaiting
                         and set v = my.RaiseAndSetIfChanged(&isWaiting , v) |> ignore
                                     my.RaisePropertyChanged(nameof(my.Runnable))
                                     my.RaisePropertyChanged(nameof(my.Stoppable))
    
    member this.Runnable = not (this.IsRunning || this.IsWaiting)
    member this.Stoppable= this.IsRunning && not this.IsWaiting
    
module TunnelConfig =
    let isRunning (config :TunnelConfig) = config.IsRunning