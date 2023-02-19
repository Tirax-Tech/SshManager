open System
open System.Diagnostics
open System.IO
open Avalonia
open Avalonia.ReactiveUI
open Serilog
open Tirax.SshManager
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
    Log.Logger <- LoggerConfiguration()
                      .WriteTo.Debug()
                      .CreateLogger()
                      
    let trace_file = Path.GetTempFileName()
    Trace.Listeners.Add(TextWriterTraceListener(trace_file)) |> ignore
    
    try
        Log.Information "Start app"
        try
            ensureSSHAgentRun()
            buildAvaloniaApp().start(args)
        with
        | e -> Trace.WriteLine $"Error occured:\n\t{e}"
               Trace.Flush()
               use p = Process.Start("notepad.exe", trace_file)
               p.WaitForExit()
               -1
    finally
        Trace.Close()
        File.Delete trace_file