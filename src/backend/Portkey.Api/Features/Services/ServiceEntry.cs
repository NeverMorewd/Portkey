    namespace Portkey.Api.Features.Services;
  public class ServiceEntry
  {
      public int Id { get; set; }
      public string Name { get; set; } = string.Empty;
      public string Address { get; set; } = string.Empty;
      public int Port { get; set; }
      public string StartCommand { get; set; } = string.Empty;
      public ServiceStatus Status { get; set; } = ServiceStatus.Stopped;
  }