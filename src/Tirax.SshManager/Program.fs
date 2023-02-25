open System
open Avalonia
open Avalonia.ReactiveUI
open Serilog
open Tirax.SshManager
open RZ.FSharp.Extension.Prelude
open RZ.FSharp.Extension.Avalonia
open Tirax.SshManager.AppConfig
open Tirax.SshManager.DI

type LiveEnv() =
    let log_file = createLogFile()
    do Log.Logger <- LoggerConfiguration().WriteTo.Debug().WriteTo.File(log_file).CreateLogger()
    
    interface AppEnvironment
    interface HasLogger with member me.logger = me
    interface HasProcess with member _.``process`` = RealProcess.Default
    interface HasFile with member _.file = RealFileIO.Default
    
    interface IDisposable with member _.Dispose() = Log.CloseAndFlush()
    
    interface DILogger with
        member _.log = Log.Logger
        member _.logFile = log_file
        member _.closeLog() = Log.CloseAndFlush()
    
    static member val Default = LiveEnv() with get
    
let [<Literal>] SshAgentProcessName = "ssh-agent"
let ensureSSHAgentRun(env: AppEnvironment) =
    let agents =
        query {
            for p in env.getProcesses() do
            where (p.ProcessName = SshAgentProcessName)
            select (p.Id, p.Responding)
        } |> Seq.toList
    assert (agents |> Seq.forall snd)
    if agents.Length = 0 then
        env.log.Information "Starting ssh-agent"
        env.startProcess(SshAgentProcessName).Dispose()
    else
        env.log.Information "ssh-agent already running"

[<CompiledName "BuildAvaloniaApp">] 
let buildAvaloniaApp env :AppBuilder =
    AppBuilder
        .Configure(fun () -> App(env))
        .UsePlatformDetect()
        .LogToTrace(areas = Array.empty)
        .UseReactiveUI()
        
[<EntryPoint; STAThread>]
let main args =
    let env: AppEnvironment = LiveEnv()
                      
    env.log.Information "Start app"
    env.log.Information("Log file path: {path}", env.logger.logFile)
    try
        ensureSSHAgentRun env
        buildAvaloniaApp(env).start(args)
        |> sideEffect (fun _ -> env.closeLog()
                                env.deleteFile env.logFile)
    with
    | e -> env.log.Fatal(e, "Unhandled exception occured!")
           env.closeLog()
           
           if OperatingSystem.IsWindows() then
               use p = env.startProcess("notepad.exe", env.logger.logFile)
               p.WaitForExit()
           -1