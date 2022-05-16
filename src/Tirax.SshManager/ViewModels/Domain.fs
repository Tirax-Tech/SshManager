namespace Tirax.SshManager.ViewModels

type Host = string
type Port = uint16

type Server = Host * Port

[<CLIMutable>]
type TunnelConfig = {
    Name :string
    SshHost :Host
    SshPort :Port
    LocalPort :Port
    RemoteHost :Host
    RemotePort :Port
}