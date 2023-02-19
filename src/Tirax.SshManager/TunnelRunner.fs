[<Microsoft.FSharp.Core.RequireQualifiedAccess>]
module Tirax.SshManager.TunnelRunner

open System.ComponentModel
open System.Diagnostics
open Akka.Actor
open RZ.FSharp.Akka
open RZ.FSharp.Extension
open Tirax.SshManager.ManagerCommands
open Tirax.SshManager.Models
open Tirax.SshManager.ViewModels

type Quited = Quited of name:string

type private ProcessCheck = ProcessCheck

let startSshProcess (tunnel :TunnelConfig) =
    let process_parameters = if tunnel.SshPort |> ServerInputFormat.isPortUnspecified
                             then [] else ["-p"; tunnel.SshPort.ToString()]
    let process_parameters = process_parameters @ ["-fN"; tunnel.SshHost; "-L"; $"{tunnel.LocalPort}:{tunnel.RemoteHost}:{tunnel.RemotePort}"]
    try
        Ok <| Process.Start("ssh", process_parameters)
    with
    | :? Win32Exception as e -> Error (e :> exn)

type Actor(parent :IActorRef, tunnel :TunnelConfig) as my =
    inherit FsReceiveActor()
    
    let mutable ssh_process :Process option = None
    
    let updateStatus state =
        let waiting, running = match state with
                               | Some v -> false, v
                               | None -> true, false
        parent.Tell (UpdateUI <| fun _ -> tunnel.IsWaiting <- waiting
                                          tunnel.IsRunning <- running)
        
    let quit (my :ActorContext) =
        parent.Tell (UpdateUI <| fun _ -> tunnel.IsWaiting <- true)
        my.Self.Tell PoisonPill.Instance
        
    let processCheck struct (my: ActorContext, _) =
        let self = my.Self
        let exit_handler _ = self.Tell PoisonPill.Instance
        match tunnel |> startSshProcess with
        | Ok p -> ssh_process <- Some p
                  p.EnableRaisingEvents <- true
                  p.Exited.Add (fun _ -> exit_handler())
                  updateStatus (Some true)
        | Error e -> Trace.WriteLine $"Start process failed with %A{e}"
                     my |> quit
                     
    do parent.Tell (UpdateUI <| fun _ -> tunnel.IsWaiting <- true)
    
    do my.Self.Tell ProcessCheck
    do my.FsReceive<ProcessCheck>(processCheck)
    
    override my.PostStop() =
        if ssh_process.IsSome then
            let p = ssh_process.Value
            if not p.HasExited then Option.safeCall p.Kill true |> ignore
            p.Dispose()
            ssh_process <- None
        parent.Tell (Quited tunnel.Name)
        updateStatus (Some false)