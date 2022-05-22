module RZ.FSharp.Akka

open System
open System.Threading.Tasks
open Akka.Actor

type ActorContext = {
    Self :IActorRef
    Sender :IActorRef
    Become :(FsActorReceivable -> unit) -> unit
}
and FsActorReceivable =
    abstract member FsReceive<'T> :(struct (ActorContext * 'T) -> unit) -> unit
    abstract member FsReceiveAsync<'T> :(struct (ActorContext * 'T) -> Async<unit>) -> unit

type FsFuncReceiver<'T> = (struct (ActorContext * 'T)) -> unit
type FsFuncAsyncReceiver<'T> = (struct (ActorContext * 'T)) -> Async<unit>

type FsReceiveActor() =
    inherit ReceiveActor()
    
    member my.FsReceive<'T>(f :FsFuncReceiver<'T>) =
        // TODO: check if Sender is really correct!
        base.Receive<'T>(fun v -> f (my.GetContext(), v))
            
    member my.FsReceiveAsync<'T>(f :FsFuncAsyncReceiver<'T>) =
        base.ReceiveAsync(fun v -> f (my.GetContext(), v) |> Async.StartImmediateAsTask :> Task)
        
    interface FsActorReceivable with
        member my.FsReceive<'T>(f :FsFuncReceiver<'T>) = my.FsReceive<'T>(f)
        member my.FsReceiveAsync<'T>(f :FsFuncAsyncReceiver<'T>) = my.FsReceiveAsync<'T>(f)
            
    member my.Become (action :Action) = base.Become action
        
    member private my.GetContext() =
        { Self = my.Self
          Sender = my.Sender
          Become = fun f -> my.Become (fun _ -> f my)}