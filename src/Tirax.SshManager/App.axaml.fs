namespace Tirax.SshManager

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml

type App() =
    inherit Application()

    override this.Initialize() =
            AvaloniaXamlLoader.Load(this)

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop ->
             let window_creator = this.DataContext :?> unit -> Window
             desktop.ShutdownMode <- ShutdownMode.OnMainWindowClose
             desktop.MainWindow <- window_creator()
        | _ -> ()

        base.OnFrameworkInitializationCompleted()