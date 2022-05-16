namespace Tirax.SshManager.ViewModels

open System

type Host = string
type Port = uint16

type Server = Host * Port

type TunnelConfig() =
    member val Name       = String.Empty with get, set
    member val SshHost    = String.Empty with get, set
    member val SshPort    = 0us          with get, set
    member val LocalPort  = 0us          with get, set
    member val RemoteHost = String.Empty with get, set
    member val RemotePort = 0us          with get, set
    
    member val IsRunning  = false        with get, set
    member val IsWaiting  = false        with get, set
    
    member this.Runnable = not (this.IsRunning || this.IsWaiting)
    member this.Stoppable= this.IsRunning && not this.IsWaiting