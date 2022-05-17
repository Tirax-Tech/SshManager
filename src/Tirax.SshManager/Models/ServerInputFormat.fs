namespace Tirax.SshManager.Models

open System
open RZ.FSharp.Extension.Prelude
open RZ.FSharp.Extension.Option

type Host = string
type Port = uint16

type Server = Host * Port

module ServerInputFormat = 
    let [<Literal>] AnyPort = 0us
    
    let parse (s :string) :Server option =
        let parts = s.Split(':', StringSplitOptions.TrimEntries)
        match parts.Length with
        | 1 -> Some (s, AnyPort)
        | 2 -> option { let! port = parseUInt16 parts[1] in return (parts[0], port) }
        | 0 | _ -> None

    let toSshServerFormat (host :Host, port :Port) :string =
        if port = AnyPort
        then host
        else $"{host}:{port}"