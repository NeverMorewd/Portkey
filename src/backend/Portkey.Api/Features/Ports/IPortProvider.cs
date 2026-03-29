namespace Portkey.Api.Features.Ports;

public interface IPortProvider
{
    /// <summary>
    /// Returns a mapping of listening port number to process ID.
    /// </summary>
    Task<Dictionary<int, int>> GetPortPidMapAsync();
}
