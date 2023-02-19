open System
open System.Diagnostics
open System.IO
open Avalonia
open Avalonia.ReactiveUI
open Serilog
open Tirax.SshManager
open RZ.FSharp.Extension.Prelude
open RZ.FSharp.Extension.Avalonia

let [<Literal>] SshAgentProcessName = "ssh-agent"
let ensureSSHAgentRun() =
    let agents =
        query {
            for p in Process.GetProcesses() do
            where (p.ProcessName = SshAgentProcessName)
            select (p.Id, p.Responding)
        } |> Seq.toList
    assert (agents |> Seq.forall snd)
    if agents.Length = 0 then
        Log.Information "Starting ssh-agent"
        Process.Start(SshAgentProcessName).Dispose()
    else
        Log.Information "ssh-agent already running"

[<CompiledName "BuildAvaloniaApp">] 
let buildAvaloniaApp() :AppBuilder =
    AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .LogToTrace(areas = Array.empty)
        .UseReactiveUI()

[<EntryPoint; STAThread>]
let main args =
    let log_file = AppConfig.createLogFile()
    Log.Logger <- LoggerConfiguration()
                      .WriteTo.Debug()
                      .WriteTo.File(log_file)
                      .CreateLogger()
                      
    Log.Information "Start app"
    Log.Information("Log file path: {path}", log_file)
    try
        ensureSSHAgentRun()
        buildAvaloniaApp().start(args)
        |> sideEffect (fun _ -> Log.CloseAndFlush()
                                File.Delete log_file)
    with
    | e -> Log.Fatal(e, "Unhandled exception occured!")
           Log.CloseAndFlush()
           
           if OperatingSystem.IsWindows() then
               use p = Process.Start("notepad.exe", log_file)
               p.WaitForExit()
           -1