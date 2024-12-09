open System
open Avalonia
open Avalonia.ReactiveUI
open Serilog
open Tirax.SshManager
open RZ.FSharp.Extension
open Tirax.SshManager.AppConfig

[<CompiledName "BuildAvaloniaApp">]
let buildAvaloniaApp () :AppBuilder =
    AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace(areas = Array.empty)
        .UseReactiveUI()

[<EntryPoint; STAThread>]
let main args =
    let log_file_path = createLogFileName()
    Log.Logger <- LoggerConfiguration()
                      .WriteTo.Debug()
                      .WriteTo.File(log_file_path)
                      .CreateLogger()
    let log = Log.Logger

    log.Information "Start app"
    log.Information("Log file path: {path}", log_file_path)
    try
        try
            Ssh.init log
            buildAvaloniaApp() |> Avalonia.start (Some args, None)
        with
        | e -> log.Fatal(e, "Unhandled exception occured!")

               if OperatingSystem.IsWindows() then
                   use p = Diagnostics.Process.Start("notepad.exe", log_file_path)
                   p.WaitForExit()
               -1
    finally
        Log.CloseAndFlush()
        IO.File.Delete(log_file_path)
