using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Portkey.Api.Hubs;

namespace Portkey.Api.Features.System;

public class SystemMetricsService : BackgroundService
{
    private readonly IHubContext<SystemHub> _hub;
    private readonly Process _process = Process.GetCurrentProcess();
    private TimeSpan _lastCpuTime;
    private DateTime _lastSampleTime;

    public SystemMetricsService(IHubContext<SystemHub> hub)
    {
        _hub = hub;
        _lastCpuTime = _process.TotalProcessorTime;
        _lastSampleTime = DateTime.UtcNow;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(2000, stoppingToken);

            _process.Refresh();
            var now = DateTime.UtcNow;
            var cpuDelta = _process.TotalProcessorTime - _lastCpuTime;
            var elapsed = now - _lastSampleTime;
            var cpuPercent = elapsed.TotalMilliseconds > 0
                ? cpuDelta.TotalMilliseconds / (elapsed.TotalMilliseconds * Environment.ProcessorCount) * 100
                : 0;

            _lastCpuTime = _process.TotalProcessorTime;
            _lastSampleTime = now;

            var metrics = new SystemMetrics(
                Math.Round(Math.Clamp(cpuPercent, 0, 100), 1),
                _process.WorkingSet64 / 1024 / 1024,
                GC.GetTotalMemory(false) / 1024 / 1024,
                now
            );

            await _hub.Clients.All.SendAsync("ReceiveMetrics", metrics, stoppingToken);
        }
    }
}
