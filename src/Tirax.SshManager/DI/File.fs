namespace Tirax.SshManager.DI

open System.IO
open System.Runtime.CompilerServices

type DIFile =
    abstract delete: string -> unit
    
type HasFile = abstract file: DIFile
    
[<IsReadOnly; Struct; NoComparison; NoEquality>]
type RealFileIO =
    static member Default = Unchecked.defaultof<RealFileIO>
    
    interface DIFile with
        member _.delete file = File.Delete file
        
[<AutoOpen>]
module File =
    type HasFile with
        member inline my.deleteFile name = my.file.delete name