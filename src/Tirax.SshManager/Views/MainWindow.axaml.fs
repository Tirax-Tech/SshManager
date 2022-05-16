namespace Tirax.SshManager.Views

open Akka.Actor
open Avalonia
open Avalonia.Controls
open Avalonia.Interactivity
open Avalonia.Markup.Xaml
open Tirax.SshManager.SshManager
open Tirax.SshManager.ViewModels

type MainWindow () as this = 
    inherit Window ()
    
    let mutable manager :IActorRef = ActorRefs.Nobody

    do this.InitializeComponent()
    
    member _.Manager with set v = manager <- v
    member private my.Model = my.DataContext :?> MainWindowViewModel

    member private this.InitializeComponent() =
#if DEBUG
        this.AttachDevTools()
#endif
        AvaloniaXamlLoader.Load(this)

    member private _.AddTunnel(_ :obj, e :RoutedEventArgs) =
        manager.Tell RegisterTunnel
        e.Handled <- true