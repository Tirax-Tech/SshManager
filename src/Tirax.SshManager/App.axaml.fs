namespace Tirax.SshManager

open Akka.Actor
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
             let main_window = MainWindow(DataContext = model, Manager = manager)
             main_window.Closing.Add(fun _ -> manager.Tell(SshManager.Quit, ActorRefs.NoSender))
             desktop.MainWindow <- main_window
        | _ -> ()

        base.OnFrameworkInitializationCompleted()