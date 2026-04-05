using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Portkey.Api.Features.Env;

public partial class EnvService
{
    private readonly byte[] _key;

    public EnvService()
    {
        _key = LoadOrCreateKey();
    }

    // ── Key management ──────────────────────────────────────────────────────

    private static byte[] LoadOrCreateKey()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Portkey");
        Directory.CreateDirectory(dir);
        var keyPath = Path.Combine(dir, "secret.key");
        if (File.Exists(keyPath))
            return File.ReadAllBytes(keyPath);
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        File.WriteAllBytes(keyPath, key);
        return key;
    }

    // ── Encryption ──────────────────────────────────────────────────────────

    public string Encrypt(string plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        using var enc = aes.CreateEncryptor();
        var pt = Encoding.UTF8.GetBytes(plaintext);
        var ct = enc.TransformFinalBlock(pt, 0, pt.Length);
        var combined = new byte[16 + ct.Length];
        aes.IV.CopyTo(combined, 0);
        ct.CopyTo(combined, 16);
        return "ENC:" + Convert.ToBase64String(combined);
    }

    public string Decrypt(string value)
    {
        if (!value.StartsWith("ENC:")) return value;
        try
        {
            var data = Convert.FromBase64String(value[4..]);
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = data[..16];
            using var dec = aes.CreateDecryptor();
            var pt = dec.TransformFinalBlock(data, 16, data.Length - 16);
            return Encoding.UTF8.GetString(pt);
        }
        catch
        {
            return value; // return as-is if decryption fails
        }
    }

    // ── File validation ──────────────────────────────────────────────────────

    [GeneratedRegex(@"^\.env(\.[a-zA-Z0-9]+)?$")]
    private static partial Regex EnvFileNameRegex();

    public static bool IsValidEnvFilename(string filename) =>
        EnvFileNameRegex().IsMatch(filename);

    // ── .env parsing ─────────────────────────────────────────────────────────

    public List<EnvEntry> ParseFile(string content, HashSet<string> sensitiveKeys)
    {
        var entries = new List<EnvEntry>();
        string? pendingComment = null;

        foreach (var rawLine in content.ReplaceLineEndings("\n").Split('\n'))
        {
            var line = rawLine.TrimEnd();
            if (string.IsNullOrEmpty(line)) { pendingComment = null; continue; }

            if (line.StartsWith('#'))
            {
                pendingComment = line[1..].Trim();
                continue;
            }

            var src = line.StartsWith("export ") ? line[7..] : line;
            var eq = src.IndexOf('=');
            if (eq < 0) continue;

            var key = src[..eq].Trim();
            var rawValue = src[(eq + 1)..];
            var value = UnquoteValue(StripInlineComment(rawValue).Trim());

            bool isSensitive = sensitiveKeys.Contains(key);
            if (value.StartsWith("ENC:"))
            {
                value = Decrypt(value);
                isSensitive = true;
            }

            entries.Add(new EnvEntry(key, value, isSensitive, pendingComment));
            pendingComment = null;
        }

        return entries;
    }

    private static string StripInlineComment(string value)
    {
        // Only strip # comments outside of quotes
        bool inSingle = false, inDouble = false;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == '\'' && !inDouble) { inSingle = !inSingle; continue; }
            if (value[i] == '"' && !inSingle) { inDouble = !inDouble; continue; }
            if (value[i] == '#' && !inSingle && !inDouble)
                return value[..i].TrimEnd();
        }
        return value;
    }

    private static string UnquoteValue(string value)
    {
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') ||
             (value[0] == '\'' && value[^1] == '\'')))
            return value[1..^1];
        return value;
    }

    // ── .env serialisation ───────────────────────────────────────────────────

    public string SerializeFile(List<EnvEntry> entries)
    {
        var sb = new StringBuilder();
        foreach (var entry in entries)
        {
            if (!string.IsNullOrEmpty(entry.Comment))
                sb.AppendLine($"# {entry.Comment}");

            var value = entry.IsSensitive && !string.IsNullOrEmpty(entry.Value)
                ? Encrypt(entry.Value)
                : entry.Value;

            if (NeedsQuoting(value))
                value = $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";

            sb.AppendLine($"{entry.Key}={value}");
        }
        return sb.ToString();
    }

    private static bool NeedsQuoting(string value) =>
        value.Contains(' ') || value.Contains('\t') || value.Contains('#') ||
        value.Contains('\'') || value.StartsWith('"');

    // ── Sidecar metadata (.portkey-meta.json) ────────────────────────────────

    public HashSet<string> LoadSensitiveKeys(string projectPath)
    {
        var metaPath = Path.Combine(projectPath, ".portkey-meta.json");
        if (!File.Exists(metaPath)) return [];
        try
        {
            var json = File.ReadAllText(metaPath);
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("sensitive", out var arr))
                return [.. arr.EnumerateArray().Select(e => e.GetString()!).Where(s => s != null)];
        }
        catch { /* ignore malformed file */ }
        return [];
    }

    public void SaveSensitiveKeys(string projectPath, IEnumerable<string> keys)
    {
        var metaPath = Path.Combine(projectPath, ".portkey-meta.json");
        var json = JsonSerializer.Serialize(new { sensitive = keys.ToArray() },
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(metaPath, json);
    }

    // ── File listing ─────────────────────────────────────────────────────────

    public List<string> ListEnvFiles(string projectPath)
    {
        if (!Directory.Exists(projectPath)) return [];
        return Directory.GetFiles(projectPath)
            .Select(Path.GetFileName)
            .Where(f => f != null && IsValidEnvFilename(f))
            .OrderBy(f => f)
            .ToList()!;
    }
}
