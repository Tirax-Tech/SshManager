module RZ.FSharp.Akka

open System
open System.Threading.Tasks
open Akka.Actor

type ActorContext =
    { Self: IActorRef
      Sender: IActorRef
      Become: (FsActorReceivable -> unit) -> unit }

and FsActorReceivable =
    abstract member FsReceive<'T> : (struct (ActorContext * 'T) -> unit) -> unit
    abstract member FsReceiveAsync<'T> : (struct (ActorContext * 'T) -> Async<unit>) -> unit

type FsFuncReceiver<'T> = (struct (ActorContext * 'T)) -> unit
type FsFuncAsyncReceiver<'T> = (struct (ActorContext * 'T)) -> Async<unit>

type UntypedHandler<'T> = (struct ('T * AkkaPackage<'T>)) -> 'T voption

and AkkaPackage<'T> =
    { Self: IActorRef
      Sender: IActorRef
      Become: UntypedHandler<'T> -> unit
      BecomeStack: UntypedHandler<'T> -> unit
      UnbecomeStack: unit -> unit
      Message: obj }

type ActorLifecycles =
    | PreStart
    | PostStop
    | PreRestart of exn * obj
    | PostRestart of exn

[<AbstractClass>]
type FsUntypedActor<'T>(s_init: 'T, h_init: UntypedHandler<'T>) =
    inherit UntypedActor()

    let mutable state = s_init
    let mutable handlers = [ h_init ]

    let become h =
        handlers <-
            match handlers with
            | [ _ ] -> [ h ]
            | _ :: rest -> h :: rest
            | [] -> failwith "impossible"

    let unbecome_stack () =
        handlers <-
            match handlers with
            | [ _ ] -> handlers
            | _ :: rest -> rest
            | [] -> failwith "impossible"

    let become_stack h = handlers <- h :: handlers

    member my.CallHandler message =
        let h = handlers |> List.head

        match h struct (state, my.GetContext(message)) with
        | ValueSome s ->
            state <- s
            true
        | ValueNone -> false

    override my.OnReceive(message) =
        if not (my.CallHandler message) then
            my.Unhandled(message)

    member private my.GetContext(m: obj) =
        { Self = my.Self
          Sender = my.Sender
          Become = become
          BecomeStack = become_stack
          UnbecomeStack = unbecome_stack
          Message = m }

    override my.PreStart() = my.CallHandler PreStart |> ignore

    override my.PostRestart(exn) =
        PostRestart(exn) |> my.CallHandler |> ignore

    override my.PostStop() = my.CallHandler PostStop |> ignore

    override my.PreRestart(exn, message) =
        PreRestart(exn, message) |> my.CallHandler |> ignore

type FsReceiveActor() =
    inherit ReceiveActor()

    member my.FsReceive<'T>(f: FsFuncReceiver<'T>) =
        // TODO: check if Sender is really correct!
        base.Receive<'T>(fun v -> f (my.GetContext(), v))

    member my.FsReceiveAsync<'T>(f: FsFuncAsyncReceiver<'T>) =
        base.ReceiveAsync(fun v -> f (my.GetContext(), v) |> Async.StartImmediateAsTask :> Task)

    interface FsActorReceivable with
        member my.FsReceive<'T>(f: FsFuncReceiver<'T>) = my.FsReceive<'T>(f)
        member my.FsReceiveAsync<'T>(f: FsFuncAsyncReceiver<'T>) = my.FsReceiveAsync<'T>(f)

    member my.Become(action: Action) = base.Become action

    member private my.GetContext() =
        { Self = my.Self
          Sender = my.Sender
          Become = fun f -> my.Become(fun _ -> f my) }