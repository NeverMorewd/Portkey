using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace Portkey.Api.Features.Ports;

public class PortScannerService
{
    private readonly IPortProvider portProvider;
    public PortScannerService(IPortProvider provider)
    {
        portProvider = provider;
    }
    public async Task<List<PortInfo>> GetListeningPortsAsync()
    {
        var portPidMap = await portProvider.GetPortPidMapAsync();
        var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();

        return listeners.Select(ep =>
        {
            portPidMap.TryGetValue(ep.Port, out var pid);
            return new PortInfo
            {
                Port = ep.Port,
                Protocol = "TCP",
                Pid = pid,
                ProcessName = GetProcessName(pid),
            };
        }).ToList();
    }
        

    private static string GetProcessName(int pid)
    {
        if (pid == 0) return "unknown";
        try
        {
            return Process.GetProcessById(pid).ProcessName;
        }
        catch
        {
            return "unknown";
        }
    }
}
