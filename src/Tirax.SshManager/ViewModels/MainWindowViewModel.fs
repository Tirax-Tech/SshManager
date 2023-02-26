namespace Tirax.SshManager.ViewModels

open System
open System.Reactive.Linq
open System.Collections.ObjectModel
open ReactiveUI

type MainWindowViewModel private () as model =
    inherit ViewModelBase()
    
    let mutable addable = Unchecked.defaultof<ObservableAsPropertyHelper<bool>>
    
    let init() =
        addable <- model.WhenAnyValue((fun x -> x.NewConnectionName),
                                      (fun (x: MainWindowViewModel) -> x.NewServerWithPort),
                                      (fun x -> x.NewLocalPort),
                                      (fun x -> x.NewDestination))
                        .Select(fun struct (name,server_port,port,dest) ->
                                 not (String.IsNullOrEmpty(name) ||
                                      String.IsNullOrEmpty(server_port) ||
                                      port = 0us ||
                                      String.IsNullOrEmpty(dest)))
                        .ToProperty(model, fun m -> m.Addable)

    let mutable newConnectionName = String.Empty
    let mutable newServerWithPort = String.Empty
    let mutable newLocalPort      = 0us
    let mutable newDestination    = String.Empty
    
    static member create() :MainWindowViewModel =
        let model = MainWindowViewModel()
        model.Init()
        model
        
    member internal _.Init() = init()
    
    member my.NewConnectionName with get() = newConnectionName and set v = my.RaiseAndSetIfChanged(&newConnectionName,v) |> ignore
    member my.NewServerWithPort with get() = newServerWithPort and set v = my.RaiseAndSetIfChanged(&newServerWithPort,v) |> ignore
    member my.NewLocalPort      with get() = newLocalPort      and set v = my.RaiseAndSetIfChanged(&newLocalPort     ,v) |> ignore
    member my.NewDestination    with get() = newDestination    and set v = my.RaiseAndSetIfChanged(&newDestination   ,v) |> ignore
    
    member val Tunnels :ObservableCollection<TunnelConfig> = ObservableCollection<TunnelConfig>() with get
    
    member _.Addable = addable.Value