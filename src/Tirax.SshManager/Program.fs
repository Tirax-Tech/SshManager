namespace Tirax.SshManager

open System
open Avalonia
open Avalonia.ReactiveUI
open Serilog
open Tirax.SshManager

module Program =
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
    let buildAvaloniaApp () = 
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .LogToTrace(areas = Array.empty)
            .UseReactiveUI()

    [<EntryPoint; STAThread>]
    let main argv =
        Log.Logger <- LoggerConfiguration()
                          .WriteTo.Debug()
                          .CreateLogger()
        
        try
            ensureSSHAgentRun()
            buildAvaloniaApp().StartWithClassicDesktopLifetime(argv)
        finally
            Log.CloseAndFlush()