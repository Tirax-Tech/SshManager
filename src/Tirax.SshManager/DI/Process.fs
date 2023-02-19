namespace Tirax.SshManager.DI

open System.Diagnostics
open System.Runtime.CompilerServices

type IProcess =
    abstract getProcesses: unit -> Process[]
    abstract member start: string -> Process
    abstract member start: string * string -> Process
    
type HasProcess =
    abstract member ``process``: IProcess
    
[<AutoOpen>]
module Process =
    type HasProcess with
        member inline my.getProcesses() = my.``process``.getProcesses()
        member inline my.startProcess (name: string) :Process = my.``process``.start name
        member inline my.startProcess (name: string, arguments: string) :Process = my.``process``.start (name, arguments)
    
[<IsReadOnly; Struct; NoComparison; NoEquality>]
type RealProcess =
    static member Default = Unchecked.defaultof<RealProcess>
    
    interface IProcess with
        member _.getProcesses() = Process.GetProcesses()
        member _.start(name: string) = Process.Start name
        member _.start(name: string, arguments: string) = Process.Start(name, arguments)