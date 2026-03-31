using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Portkey.Api.Data;
using Portkey.Api.Hubs;

namespace Portkey.Api.Features.Services;

public class ServiceManager
{
    private readonly PortkeyDbContext _dbContext;
    private readonly ConcurrentDictionary<int, Process> _runningProcesses = new();
    private readonly ILogger<ServiceManager> _logger;
    private readonly IHubContext<LogHub> _logHubContext;
    private readonly IServiceScopeFactory _scopeFactory;
    public ServiceManager(PortkeyDbContext dbContext,
    IHubContext<LogHub> logHubContext,
    ILogger<ServiceManager> logger,
    IServiceScopeFactory scopeFactory)
    {
        _dbContext = dbContext;
        _logger = logger;
        _logHubContext = logHubContext;
        _scopeFactory = scopeFactory;
    }

    public async Task<IEnumerable<ServiceEntry>> GetServices()
    {
        return await _dbContext.ServiceEntries.ToListAsync();
    }
    public async Task<ServiceEntry?> AddService(ServiceEntry service)
    {
        try
        {
            _dbContext.ServiceEntries.Add(service);
            await _dbContext.SaveChangesAsync();
            return service;
        }
        catch (Exception ex)
        {
            //todo log
            return null;
        }

    }
    public async Task<bool> DeleteService(int id)
    {
        try
        {
            _dbContext.ServiceEntries.Remove(new ServiceEntry { Id = id });
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            //todo log
            return false;
        }
    }

    public async Task<bool> StopService(int id)
    {
        var serviceEntry = await _dbContext.ServiceEntries.Where(e => e.Id == id).FirstAsync();
        try
        {
            if (_runningProcesses.TryRemove(id, out var process))
            {
                process.Kill();
                await process.WaitForExitAsync();
                serviceEntry.Status = ServiceStatus.Stopped;
                _dbContext.ServiceEntries.Update(serviceEntry);
                await _dbContext.SaveChangesAsync();
                await _logHubContext.Clients.Group($"service-{id}").SendAsync("ReceiveLog", new { id, stream = "exited", line = $"exited:{process.ExitCode}", timestamp = DateTime.UtcNow });
                process.Dispose();
            }
            return true;
        }
        catch (Exception ex)
        {
            //todo log
            return false;
        }
    }
    public async Task<bool> StartService(int id)
    {
        var serviceEntry = await _dbContext.ServiceEntries.Where(e => e.Id == id).FirstAsync();
        try
        {
            if (_runningProcesses.ContainsKey(id))
                return true; //already running
            var parts = serviceEntry.StartCommand.Split(' ', 2);
            var psi = new ProcessStartInfo(parts[0], parts.Length > 1 ? parts[1] : "");
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            var process = Process.Start(psi)!;
            if (process == null)
                return false;
            _ = ReadStreamAsync(process.StandardOutput, id, "stdout");
            _ = ReadStreamAsync(process.StandardError, id, "stderr");
            process.EnableRaisingEvents = true;
            process.Exited += async (s, e) =>
            {
                if (_runningProcesses.TryRemove(id, out var processtoRemove))
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<PortkeyDbContext>();
                    var entry = db.ServiceEntries.Find(id);
                    if (entry is not null)
                    {
                        entry.Status = ServiceStatus.Stopped;
                        db.SaveChanges();
                    }
                    await _logHubContext.Clients.All.SendAsync("ServiceStatusChanged", new { id, status = "Stopped" });
                    await _logHubContext.Clients.Group($"service-{id}").SendAsync("ReceiveLog", new { id, stream = "exited", line = $"exited:{processtoRemove.ExitCode}", timestamp = DateTime.UtcNow });
                    processtoRemove.Dispose();
                }
            };
            _runningProcesses.TryAdd(serviceEntry.Id, process!);
            serviceEntry.Status = ServiceStatus.Running;
            _dbContext.ServiceEntries.Update(serviceEntry);
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start service {ServiceId}", id);
            return false;
        }
    }
    public async Task<bool> RestartService(int id)
    {
        throw new NotImplementedException();
    }

    private async Task ReadStreamAsync(StreamReader reader, int serviceId, string stream)
    {
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (line is not null)
            {
                await _logHubContext.Clients.Group($"service-{serviceId}")
                    .SendAsync("ReceiveLog", new { serviceId, stream, line, timestamp = DateTime.UtcNow });
            }
        }
    }

}