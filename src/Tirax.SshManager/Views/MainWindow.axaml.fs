namespace Tirax.SshManager.Views

open System
open Avalonia
open Avalonia.Controls
open Avalonia.Interactivity
open Avalonia.Markup.Xaml
open RZ.FSharp.Extension.Prelude
open RZ.FSharp.Extension.Option
open Tirax.SshManager.ViewModels

module MainWindow =
    let [<Literal>] AnyPort = 0us
    
    let parseServer (s :string) =
        let parts = s.Split(':', StringSplitOptions.TrimEntries)
        match parts.Length with
        | 1 -> Some (s, AnyPort)
        | 2 -> option { let! port = parseUInt16 parts[1] in return (parts[0], port) }
        | 0 | _ -> None

type MainWindow () as this = 
    inherit Window ()

    do this.InitializeComponent()
    
    member private my.Model = my.DataContext :?> MainWindowViewModel

    member private this.InitializeComponent() =
#if DEBUG
        this.AttachDevTools()
#endif
        AvaloniaXamlLoader.Load(this)
        printfn "Inited!"

    member private this.AddTunnel(sender :obj, e :RoutedEventArgs) =
        let model = this.Model
        let ssh_server = MainWindow.parseServer model.NewServerWithPort
        if ssh_server.IsNone then failwithf $"Invalid SSH server format: %s{model.NewServerWithPort}"
        let (ssh_host, ssh_port) = ssh_server.Value
        
        if model.NewLocalPort < 1us then failwithf $"Invalid local port: %d{model.NewLocalPort}"
        
        let remote_server = MainWindow.parseServer model.NewDestination
        if remote_server.IsNone then failwithf $"Invalid destination server format: %s{model.NewDestination}"
        let (remote_host, remote_port) = remote_server.Value
        
        let tunnel = TunnelConfig(
            SshHost = ssh_host,
            SshPort = ssh_port,
            LocalPort = model.NewLocalPort,
            RemoteHost = remote_host,
            RemotePort = remote_port
        )
        model.Tunnels.Add tunnel