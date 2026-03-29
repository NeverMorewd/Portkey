using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace Portkey.Api.Features.Ports.Providers;

[SupportedOSPlatform("windows")]
public class WindowsPortProvider : IPortProvider
{
    public async Task<Dictionary<int, int>> GetPortPidMapAsync()
    {
        var map = new Dictionary<int, int>();
        var output = await RunCommandAsync("netstat", "-ano");

        foreach (var line in output.Split('\n'))
        {
            var match = Regex.Match(line.Trim(), @"^TCP\s+\S+:(\d+)\s+\S+\s+LISTENING\s+(\d+)");
            if (match.Success)
            {
                var port = int.Parse(match.Groups[1].Value);
                var pid = int.Parse(match.Groups[2].Value);
                map.TryAdd(port, pid);
            }
        }
        return map;
    }

    private static async Task<string> RunCommandAsync(string cmd, string args)
    {
        try
        {
            var psi = new ProcessStartInfo(cmd, args)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = Process.Start(psi)!;
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return output;
        }
        catch { return string.Empty; }
    }
}
