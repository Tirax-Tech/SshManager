namespace Tirax.SshManager

open System
open Avalonia
open Avalonia.ReactiveUI
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
            System.Diagnostics.Process.Start(SshAgentProcessName).Dispose()

    [<CompiledName "BuildAvaloniaApp">] 
    let buildAvaloniaApp () = 
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .LogToTrace(areas = Array.empty)
            .UseReactiveUI()

    [<EntryPoint; STAThread>]
    let main argv =
        ensureSSHAgentRun()
        buildAvaloniaApp().StartWithClassicDesktopLifetime(argv)