using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace Portkey.Api.Features.Ports.Providers;

[SupportedOSPlatform("linux")]
public class LinuxPortProvider : IPortProvider
{
    public async Task<Dictionary<int, int>> GetPortPidMapAsync()
    {
        var map = new Dictionary<int, int>();
        // ss -tlnp output example:
        // tcp  LISTEN 0  128  0.0.0.0:22  0.0.0.0:*  users:(("sshd",pid=1234,fd=3))
        var output = await RunCommandAsync("ss", "-tlnp");

        foreach (var line in output.Split('\n'))
        {
            var portMatch = Regex.Match(line, @"\*:(\d+)|0\.0\.0\.0:(\d+)|\[::\]:(\d+)");
            var pidMatch = Regex.Match(line, @"pid=(\d+)");

            if (portMatch.Success && pidMatch.Success)
            {
                var portStr = portMatch.Groups[1].Value.Length > 0
                    ? portMatch.Groups[1].Value
                    : portMatch.Groups[2].Value.Length > 0
                        ? portMatch.Groups[2].Value
                        : portMatch.Groups[3].Value;

                if (int.TryParse(portStr, out var port) && int.TryParse(pidMatch.Groups[1].Value, out var pid))
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
