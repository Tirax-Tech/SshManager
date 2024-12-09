module RZ.FSharp.Extension.Avalonia

open System
open System.Linq.Expressions
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Reactive.Disposables
open ReactiveUI
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes

let start (args: string array option, shutdown_mode: ShutdownMode option) (app: AppBuilder) :int =
    use lifetime = ClassicDesktopStyleApplicationLifetime(ShutdownMode = shutdown_mode.defaultValue ShutdownMode.OnLastWindowClose)
    app.SetupWithLifetime lifetime |> ignore
    lifetime.Start (args.defaultValue Array.empty)

type Control with
    member inline my.findControl<'T when 'T :not struct
                                     and 'T :null
                                     and 'T :> Control>([<CallerMemberName;
                                                          Optional;
                                                          DefaultParameterValue("")>]
                                                         name: string) :'T option =
        Option.ofObj <| my.FindControl<'T>(name)

type MVVM<'view, 'vm when 'view :> IViewFor<'vm>
                     and 'view: not struct
                     and 'vm: not struct>(v: 'view,vm: 'vm,disposable) =
    member me.bind(vm_target: Expression<Func<'vm,'a>>, v_target: Expression<Func<'view,'b>>) :MVVM<'view,'vm> =
        v.Bind(vm, vm_target, v_target).DisposeWith(disposable) |> ignore
        me

    member me.bindCommand(command_target: Expression<Func<'vm,'command>>, v_target: Expression<Func<'view,'control>>) :MVVM<'view,'vm> =
        v.BindCommand(vm, command_target, v_target).DisposeWith(disposable) |> ignore
        me

    member inline me.``end``() = ()