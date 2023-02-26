namespace Tirax.SshManager.Views

open Akka.Actor
open ReactiveUI
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Mixins
open Avalonia.Input
open Avalonia.Interactivity
open Avalonia.Markup.Xaml
open Avalonia.ReactiveUI
open RZ.FSharp.Extension
open RZ.FSharp.Extension.Avalonia
open Tirax.SshManager
open Tirax.SshManager.AppConfig
open Tirax.SshManager.SshManager
open Tirax.SshManager.ViewModels

type MainWindow (env: AppEnvironment) as this = 
    inherit ReactiveWindow<MainWindowViewModel>()
    
    let mutable manager :IActorRef = ActorRefs.Nobody

    do this.WhenActivated(fun disposables ->
           MVVM(this, this.ViewModel, disposables)
               .bind((fun vm -> vm.NewConnectionName), (fun v -> v.inpNewConnectionName.Text))
               .bind((fun vm -> vm.NewServerWithPort), (fun v -> v.inpNewServerWithPort.Text))
               .bind((fun vm -> vm.NewLocalPort     ), (fun v -> v.inpNewLocalPort     .Text))
               .bind((fun vm -> vm.NewDestination   ), (fun v -> v.inpNewDestination   .Text))
               .bind((fun vm -> vm.Addable), (fun v -> v.btnAddTunnel.IsEnabled))
               .``end``()
       ) |> ignore
    do this.InitializeComponent()
    do this.Title <- AppConfig.Title.Value
    
    // Design Time only!
    new() = MainWindow(Unchecked.defaultof<AppEnvironment>)
    
    member my.inpNewConnectionName :TextBox = my.findControl<TextBox>().unwrap()
    member my.inpNewServerWithPort :TextBox = my.findControl<TextBox>().unwrap()
    member my.inpNewLocalPort      :TextBox = my.findControl<TextBox>().unwrap()
    member my.inpNewDestination    :TextBox = my.findControl<TextBox>().unwrap()
    member my.btnAddTunnel         :Button  = my.findControl<Button>().unwrap()
    
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