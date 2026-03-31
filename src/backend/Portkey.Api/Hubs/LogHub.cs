using Microsoft.AspNetCore.SignalR;

  namespace Portkey.Api.Hubs;

  public class LogHub : Hub
  {
      public async Task JoinServiceLog(string serviceId)
      {
          await Groups.AddToGroupAsync(Context.ConnectionId, $"service-{serviceId}");
      }

      public async Task LeaveServiceLog(string serviceId)
      {
          await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"service-{serviceId}");
      }
  }
