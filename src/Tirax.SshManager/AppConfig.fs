module Tirax.SshManager.AppConfig

open System.Reflection

let Version = lazy (let version = Option.ofObj <| Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    in version |> Option.map (fun v -> v.InformationalVersion))
let Title = lazy (let version = match Version.Value with
                                | Some v -> $"v{v}"
                                | None -> "(unspecified)"
                  in $"Tirax SSH Manager {version}")