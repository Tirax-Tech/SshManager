namespace Tirax.SshManager.ViewModels

open System
open System.Collections.ObjectModel

type MainWindowViewModel() =
    inherit ViewModelBase()

    member this.Greeting = "Welcome to Avalonia!"

    member val NewServerWithPort = String.Empty with get, set
    member val NewLocalPort = 0us with get, set
    member val NewDestination = String.Empty with get, set
    
    member val Tunnels :ObservableCollection<TunnelConfig> = ObservableCollection<TunnelConfig>() with get