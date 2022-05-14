module Tirax.SshManager.Models.Domain

open System
open ReactiveUI

type Host = string
type Port = int16

type Server = Host * Port

type TunnelConfig() =
    inherit ReactiveObject()
    
    let mutable ssh_host = String.Empty
    let mutable ssh_port = 0s
    let mutable local_port = 0s
    let mutable remote_host = String.Empty
    let mutable remote_port = 0s
    
    member my.SshHost with get() = ssh_host and set v = my.RaiseAndSetIfChanged(&ssh_host, v) |> ignore
    member my.SshPort with get() = ssh_port and set v = my.RaiseAndSetIfChanged(&ssh_port, v) |> ignore
    member my.LocalPort with get() = local_port and set v = my.RaiseAndSetIfChanged(&local_port, v) |> ignore
    member my.RemoteHost with get() = remote_host and set v = my.RaiseAndSetIfChanged(&remote_host, v) |> ignore
    member my.RemotePort with get() = remote_port and set v = my.RaiseAndSetIfChanged(&remote_port, v) |> ignore