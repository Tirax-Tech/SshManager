module RZ.FSharp.Extension.Avalonia

open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes

type AppBuilder with
    member inline my.start(?args, ?shutdown_mode) :int =
        use lifetime = ClassicDesktopStyleApplicationLifetime
                           (ShutdownMode = shutdown_mode.defaultValue ShutdownMode.OnLastWindowClose)
        my.SetupWithLifetime lifetime |> ignore
        lifetime.Start (args.defaultValue Array.empty)

type IControl with
    member inline my.findControl<'T when 'T :not struct
                                     and 'T :null
                                     and 'T :> IControl>([<CallerMemberName;
                                                           Optional;
                                                           DefaultParameterValue("")>]
                                                         name: string) :'T option =
        Option.ofObj <| my.FindControl<'T>(name)