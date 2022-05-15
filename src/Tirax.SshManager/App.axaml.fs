namespace Tirax.SshManager

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml
open Tirax.SshManager.ViewModels
open Tirax.SshManager.Views

type App() =
    inherit Application()

    override this.Initialize() =
            AvaloniaXamlLoader.Load(this)

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop ->
             let model = MainWindowViewModel()
             desktop.MainWindow <- MainWindow(DataContext = model)
             let manager = SshManager.init model
             desktop.Exit.Add(fun _ -> manager |> SshManager.shutdown |> Async.RunSynchronously)
        | _ -> ()

        base.OnFrameworkInitializationCompleted()