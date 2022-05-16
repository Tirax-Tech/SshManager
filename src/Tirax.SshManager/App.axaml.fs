namespace Tirax.SshManager

open System
open System.Threading.Tasks
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
             let manager = SshManager.init model
             desktop.MainWindow <- MainWindow(DataContext = model, Manager = manager)
             let shutdown = Func<_>(fun _ -> manager |> SshManager.shutdown)
             desktop.Exit.Add(fun _ -> Task.Run<unit>(shutdown) |> ignore)
        | _ -> ()

        base.OnFrameworkInitializationCompleted()