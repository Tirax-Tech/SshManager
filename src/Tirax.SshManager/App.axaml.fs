namespace Tirax.SshManager

open Akka.Actor
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml
open Tirax.SshManager.ViewModels
open Tirax.SshManager.Views

// Avalonia design based on single app, run once. So one process cannot start more than one app.
// Meaning all windows needed should be handled in App scope.
// For example, it's not possible to show "Notepad" window app after the main window app is completed. Both windows
// must reside in the same app.
//
type App() =
    inherit Application()
    
    override this.Initialize() =
            AvaloniaXamlLoader.Load(this)

    override this.OnFrameworkInitializationCompleted() =
        let model = MainWindowViewModel()
        let manager = SshManager.init model
        
        match this.ApplicationLifetime with
        | :?IClassicDesktopStyleApplicationLifetime as desktop ->
            desktop.ShutdownMode <- ShutdownMode.OnMainWindowClose
            desktop.MainWindow <- MainWindow(DataContext = model, Manager = manager)
            desktop.ShutdownRequested.Add(fun _ -> manager.Tell(SshManager.Quit, ActorRefs.NoSender))
            
        | _ -> ()

        base.OnFrameworkInitializationCompleted()