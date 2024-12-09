module Tirax.SshManager.Ssh

open System.Diagnostics
open Serilog

let [<Literal>] private SshAgentProcessName = "ssh-agent"

let private getRunningSshAgent () =
    let getAgents(processes: Process array) =
        query {
            for p in processes do
            where (p.ProcessName = SshAgentProcessName)
            select (p.Id, p.Responding)
        } |> Seq.toList

    let processes = Process.GetProcesses()
    let agents = processes |> getAgents
    assert (agents |> Seq.forall snd)
    agents.Length > 0

let private startSshAgent () =
    Process.Start(SshAgentProcessName).Dispose()

let private startSshAgentIfNeeded () =
    let has_agent = getRunningSshAgent()
    if not has_agent then
        startSshAgent()

let init(logger: ILogger) =
    startSshAgentIfNeeded()

    logger.Information "SSH module initialized."
