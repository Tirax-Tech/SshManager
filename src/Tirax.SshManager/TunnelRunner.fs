[<Microsoft.FSharp.Core.RequireQualifiedAccess>]
module Tirax.SshManager.TunnelRunner

open System.ComponentModel
open System.Diagnostics
open Akka.Actor
open RZ.FSharp.Extension
open Tirax.SshManager.Models
open Tirax.SshManager.ViewModels

type RunOk = RunOk of tunnel:TunnelConfig
type RunFailure = RunFailure of tunnel:TunnelConfig * error:string
type Quited = Quited of name:string

type private ProcessCheck = ProcessCheck

let startSshProcess (tunnel :TunnelConfig) =
    let ssh_server = ServerInputFormat.toSshServerFormat (tunnel.SshHost, tunnel.SshPort)
    try
        let p = Process.Start("ssh", ["-fN"; ssh_server; "-L"; $"{tunnel.LocalPort}:{tunnel.RemoteHost}:{tunnel.RemotePort}"])
        if p.HasExited then Error (exn "Process has exited")
        else Ok p
    with
    | :? Win32Exception as e -> Error (e :> exn)

type Actor(parent :IActorRef, tunnel :TunnelConfig) as my =
    inherit ReceiveActor()
    
    let mutable ssh_process :Process option = None
    
    do my.Self.Tell ProcessCheck
    do my.Receive<ProcessCheck>(my.ProcessCheck)
    
    override my.PostStop() =
        if ssh_process.IsSome then
            let p = ssh_process.Value
            if not p.HasExited then Option.safeCall p.Kill true |> ignore
            p.Dispose()
            ssh_process <- None
        parent.Tell (Quited tunnel.Name)

    member private my.ProcessCheck _ :unit =
        match startSshProcess tunnel with
        | Ok p -> ssh_process <- Some p
                  parent.Tell <| RunOk tunnel
        | Error e -> parent.Tell <| RunFailure (tunnel, e.Message)
                     my.Self.Tell PoisonPill.Instance