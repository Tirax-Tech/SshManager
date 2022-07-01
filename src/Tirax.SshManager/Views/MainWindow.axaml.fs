namespace Tirax.SshManager.Views

open Akka.Actor
open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Interactivity
open Avalonia.Markup.Xaml
open Tirax.SshManager
open Tirax.SshManager.SshManager
open Tirax.SshManager.ViewModels

type MainWindow () as this = 
    inherit Window ()
    
    let mutable manager :IActorRef = ActorRefs.Nobody

    do this.InitializeComponent()
    do this.Title <- AppConfig.Title.Value
    
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
        
    member private _.RunTunnel(sender :obj, e :RoutedEventArgs) =
        let button = sender :?> Button
        let tunnel = button.Tag :?> TunnelConfig
        manager.Tell (RunTunnel tunnel)
        e.Handled <- true
        
    member private _.StopTunnel(sender :obj, e :RoutedEventArgs) =
        let button = sender :?> Button
        let tunnel = button.Tag :?> TunnelConfig
        manager.Tell (StopTunnel tunnel.Name)
        e.Handled <- true
        
    member private my.GridKeyUp(sender :obj, key :KeyEventArgs) =
        if key.Key = Key.Delete && key.KeyModifiers = KeyModifiers.None then
            let tunnel = (sender :?> DataGrid).SelectedItem :?> TunnelConfig
            manager.Tell (UnregisterTunnel tunnel.Name)
            key.Handled <- true
            
    member private _.EditingCells(_ :obj, e :DataGridBeginningEditEventArgs) =
        let data = e.Row.DataContext :?> TunnelConfig
        data.IsEditing <- true
        
    member private _.CellEditingEnded(_ :obj, e :DataGridCellEditEndedEventArgs) =
        let data = e.Row.DataContext :?> TunnelConfig
        data.IsEditing <- false
        manager.Tell (StopTunnel data.Name)
        manager.Tell UpdateModel