open System
open System.Threading
open System.Threading.Tasks
open Akka.Actor
open Avalonia
open Avalonia.Controls
open Avalonia.ReactiveUI
open Serilog
open Tirax.SshManager
open Tirax.SshManager.ViewModels
open Tirax.SshManager.Views
open RZ.FSharp.Extension.Prelude

let [<Literal>] SshAgentProcessName = "ssh-agent"
let ensureSSHAgentRun() =
    let agents =
        query {
            for p in System.Diagnostics.Process.GetProcesses() do
            where (p.ProcessName = SshAgentProcessName)
            select (p.Id, p.Responding)
        } |> Seq.toList
    assert (agents |> Seq.forall snd)
    if agents.Length = 0 then
        Log.Information "Starting ssh-agent"
        System.Diagnostics.Process.Start(SshAgentProcessName).Dispose()
    else
        Log.Information "ssh-agent already running"

[<CompiledName "BuildAvaloniaApp">] 
let buildAvaloniaApp (window_creator: unit -> Window) :AppBuilder =
    let app = App()
    app.DataContext <- window_creator
    AppBuilder
        .Configure(constant app)
        .UsePlatformDetect()
        .LogToTrace(areas = Array.empty)
        .UseReactiveUI()

[<EntryPoint; STAThread>]
let main argv =
    Log.Logger <- LoggerConfiguration()
                      .WriteTo.Debug()
                      .CreateLogger()
                      
    SynchronizationContext.SetSynchronizationContext(SynchronizationContext())
    let model = MainWindowViewModel()
    let manager = SshManager.init model
    let createSshWindow() :Window = MainWindow(DataContext = model, Manager = manager)
    try
        ensureSSHAgentRun()
        let result = buildAvaloniaApp(createSshWindow).StartWithClassicDesktopLifetime(argv)
        assert (result = 0)
        manager.Tell(SshManager.Quit, ActorRefs.NoSender)
        0
    finally
        Log.CloseAndFlush()