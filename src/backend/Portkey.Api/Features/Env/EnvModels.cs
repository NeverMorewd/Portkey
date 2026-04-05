namespace Portkey.Api.Features.Env;

public record EnvEntry(string Key, string Value, bool IsSensitive, string? Comment = null);
