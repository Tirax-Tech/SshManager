namespace Tirax.SshManager

type HostName = string
type NetworkPort = uint16

type TunnelConfig = {
    Name: string
    Host: HostName
    Port: NetworkPort
    LocalPort: NetworkPort
    RemoteHost: HostName
    RemotePort: NetworkPort
}

type SshManagerService =
    abstract start: TunnelConfig -> unit
    abstract stop: TunnelConfig -> unit
