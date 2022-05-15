module Tirax.SshManager.SshManager

open System.Diagnostics
open System.IO
open Akka.Actor
open Akka.Configuration
open Tirax.SshManager.ViewModels

let private config = File.ReadAllText "config.hocon" |> ConfigurationFactory.ParseString
let Actor = ActorSystem.Create("SshManager", config)

type SshManager(model :MainWindowViewModel) =
    inherit ReceiveActor()
    
    override _.PreStart() =
        printfn "START!!!"
        Debug.WriteLine "START JAAAAAAAAAA"

let init (model :MainWindowViewModel) =
    Actor.ActorOf(Props.Create<SshManager>(model), "ssh-manager")
    
let shutdown (actor :IActorRef) = async {
    actor.Tell(PoisonPill.Instance)
    do! Actor.Terminate() |> Async.AwaitTask
}