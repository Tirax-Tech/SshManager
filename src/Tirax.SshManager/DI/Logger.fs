namespace Tirax.SshManager.DI

open Serilog

type DILogger =
    abstract logFile: string
    abstract log: ILogger
    abstract closeLog: unit -> unit

type HasLogger = abstract logger: DILogger

[<AutoOpen>]
module Logger =
    type HasLogger with
        member inline my.logFile :string = my.logger.logFile
        member inline my.log :ILogger = my.logger.log
        member inline my.closeLog() = my.logger.closeLog()