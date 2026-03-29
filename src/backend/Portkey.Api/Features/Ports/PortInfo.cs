namespace Portkey.Api.Features.Ports;

public record PortInfo
{
    public int Port { get; init; }
    public int Pid { get; init; }
    public string ProcessName { get; init; } = string.Empty;
    public string Protocol { get; init; } = string.Empty;
}
