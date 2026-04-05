namespace Portkey.Api.Features.System;

public record SystemMetrics(
    double CpuPercent,
    long WorkingSetMb,
    long ManagedMemoryMb,
    DateTime Timestamp
);
