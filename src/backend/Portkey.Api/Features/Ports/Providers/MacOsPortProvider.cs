using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace Portkey.Api.Features.Ports.Providers;

[SupportedOSPlatform("macos")]
public class MacOsPortProvider : IPortProvider
{
    public async Task<Dictionary<int, int>> GetPortPidMapAsync()
    {
        var map = new Dictionary<int, int>();
        // lsof output example:
        // node  1234  user  23u  IPv4  0x...  0t0  TCP *:3000 (LISTEN)
        var output = await RunCommandAsync("lsof", "-i -P -n -sTCP:LISTEN");

        foreach (var line in output.Split('\n').Skip(1)) // skip header
        {
            var parts = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 9) continue;

            var portMatch = Regex.Match(parts[^2], @":(\d+)$");
            if (portMatch.Success && int.TryParse(parts[1], out var pid) && int.TryParse(portMatch.Groups[1].Value, out var port))
                map.TryAdd(port, pid);
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
