namespace Tirax.SshManager.Models

open System
open RZ.FSharp.Extension

type Host = string
type Port = uint16

type Server = Host * Port

module ServerInputFormat =
    let [<Literal>] private Unspecified = 0us

    let inline isPortUnspecified port = port = Unspecified

    let parse (s :string) :Server voption =
        let parts = s.Split(':', StringSplitOptions.TrimEntries)
        match parts.Length with
        | 1 -> ValueSome (s, Unspecified)
        | 2 -> voption { let! port = parseUInt16 parts[1] in return (parts[0], port) }
        | 0 | _ -> ValueNone

    let toSshServerFormat (host :Host, port :Port) :string =
        if port |> isPortUnspecified then host else $"{host}:{port}"