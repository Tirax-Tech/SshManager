module Tirax.SshManager.AppConfig

open System
open System.IO
open System.Reflection
open type System.Environment

let Version = lazy (let version = Option.ofObj <| Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    in version |> Option.map (fun v -> v.InformationalVersion))
let Title = lazy (let version = match Version.Value with
                                | Some v -> $"v{v}"
                                | None -> "(unspecified)"
                  in $"Tirax SSH Manager {version}")

let [<Literal>] AppName = "TiraxSshManager"
let private local_app_folder :string =
    if OperatingSystem.IsWindows()
    then Path.Combine(GetFolderPath SpecialFolder.LocalApplicationData, AppName)
    else Path.Combine(GetFolderPath SpecialFolder.UserProfile, $".{AppName}")
    
let private log_folder :string = Path.Combine(local_app_folder, "logs")

if not <| Directory.Exists log_folder then Directory.CreateDirectory log_folder |> ignore
    
let AppPath = local_app_folder

let logFile file_name :string = Path.Combine(log_folder, file_name)
let createLogFile() =
    let now = DateTime.Now.ToString("yyyyMMdd-HHmmss")
    in  logFile $"log-%s{now}.txt"

type AppEnvironment = interface end