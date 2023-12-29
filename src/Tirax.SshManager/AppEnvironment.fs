namespace Tirax.SshManager

open Tirax.SshManager.DI
open Tirax.SshManager.ViewModels

type ObjStorage =
    abstract load<'T> :unit -> Async<'T>
    abstract save<'T> :'T -> Async<unit>
    
type SshManagerService =
    abstract start: TunnelConfig -> unit
    abstract stop: TunnelConfig -> unit
    
type HasObjStorage = abstract storage: ObjStorage
type HasSshManager = abstract manager: SshManagerService

type AppEnvironment =
    inherit HasFile
    inherit HasLogger
    inherit HasProcess
    inherit HasObjStorage
    inherit HasSshManager