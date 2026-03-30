using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Portkey.Api.Data;

namespace Portkey.Api.Features.Services;

public class ServiceManager
{
    private readonly PortkeyDbContext _dbContext;
    private readonly ConcurrentDictionary<int, Process> _runningProcesses = new();
    private readonly ILogger<ServiceManager> _logger;
    public ServiceManager(PortkeyDbContext dbContext, ILogger<ServiceManager> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
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
            var process = Process.Start(psi)!;
            if (process == null)
                return false;
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) =>
            {
                if (_runningProcesses.TryRemove(id, out _))
                {
                    serviceEntry.Status = ServiceStatus.Stopped;
                    _dbContext.ServiceEntries.Update(serviceEntry);
                    _dbContext.SaveChanges();
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

}