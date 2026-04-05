using Portkey.Api.Features.Env;

namespace Portkey.Tests;

public class EnvServiceTests
{
    private readonly EnvService _svc = new();

    // ── Filename validation ───────────────────────────────────────────────────

    [Theory]
    [InlineData(".env",             true)]
    [InlineData(".env.development", true)]
    [InlineData(".env.prod",        true)]
    [InlineData(".env.staging123",  true)]
    [InlineData("env",              false)]
    [InlineData(".envrc",           false)]
    [InlineData(".env.my.file",     false)]
    [InlineData("",                 false)]
    [InlineData(".env/evil",        false)]
    public void IsValidEnvFilename(string name, bool expected)
    {
        Assert.Equal(expected, EnvService.IsValidEnvFilename(name));
    }

    // ── Parsing ───────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_SimpleKeyValue()
    {
        var entries = _svc.ParseFile("FOO=bar\nBAZ=qux\n", []);

        Assert.Equal(2, entries.Count);
        Assert.Equal("FOO", entries[0].Key);
        Assert.Equal("bar", entries[0].Value);
        Assert.Equal("BAZ", entries[1].Key);
        Assert.Equal("qux", entries[1].Value);
    }

    [Fact]
    public void Parse_QuotedValues()
    {
        var entries = _svc.ParseFile("A=\"hello world\"\nB='single quoted'\n", []);

        Assert.Equal("hello world", entries[0].Value);
        Assert.Equal("single quoted", entries[1].Value);
    }

    [Fact]
    public void Parse_SkipsCommentLines()
    {
        var entries = _svc.ParseFile("# this is a comment\nKEY=val\n", []);

        Assert.Single(entries);
        Assert.Equal("KEY", entries[0].Key);
    }

    [Fact]
    public void Parse_AttachesCommentToPrecedingEntry()
    {
        var entries = _svc.ParseFile("# my comment\nKEY=val\n", []);

        Assert.Equal("my comment", entries[0].Comment);
    }

    [Fact]
    public void Parse_ExportPrefix()
    {
        var entries = _svc.ParseFile("export API_KEY=secret\n", []);

        Assert.Single(entries);
        Assert.Equal("API_KEY", entries[0].Key);
        Assert.Equal("secret", entries[0].Value);
    }

    [Fact]
    public void Parse_MarksKnownSensitiveKeys()
    {
        var entries = _svc.ParseFile("DB_PASS=abc\nOTHER=xyz\n", ["DB_PASS"]);

        Assert.True(entries[0].IsSensitive);
        Assert.False(entries[1].IsSensitive);
    }

    [Fact]
    public void Parse_IgnoresBlankLines()
    {
        var entries = _svc.ParseFile("\nFOO=bar\n\n\nBAZ=qux\n\n", []);

        Assert.Equal(2, entries.Count);
    }

    // ── Encrypt / decrypt roundtrip ───────────────────────────────────────────

    [Fact]
    public void EncryptDecrypt_Roundtrip()
    {
        var plain = "super-secret-password-123!";
        var encrypted = _svc.Encrypt(plain);

        Assert.StartsWith("ENC:", encrypted);
        Assert.NotEqual(plain, encrypted);

        var decrypted = _svc.Decrypt(encrypted);
        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public void Decrypt_ReturnsOriginal_IfNotEncrypted()
    {
        var plain = "not-encrypted";
        Assert.Equal(plain, _svc.Decrypt(plain));
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertext_EachTime()
    {
        var plain = "same-value";
        var c1 = _svc.Encrypt(plain);
        var c2 = _svc.Encrypt(plain);
        Assert.NotEqual(c1, c2); // different IVs each time
    }

    // ── Serialisation ─────────────────────────────────────────────────────────

    [Fact]
    public void Serialize_ThenParse_Roundtrip()
    {
        var original = new List<EnvEntry>
        {
            new("KEY1", "value1", false),
            new("KEY2", "value with spaces", false),
            new("SECRET", "topsecret", true, "my secret key"),
        };

        var serialized = _svc.SerializeFile(original);
        var sensitiveKeys = original.Where(e => e.IsSensitive).Select(e => e.Key).ToHashSet();
        var parsed = _svc.ParseFile(serialized, sensitiveKeys);

        Assert.Equal(original.Count, parsed.Count);
        Assert.Equal("KEY1",   parsed[0].Key);
        Assert.Equal("value1", parsed[0].Value);
        Assert.Equal("value with spaces", parsed[1].Value);
        Assert.Equal("topsecret", parsed[2].Value);
        Assert.True(parsed[2].IsSensitive);
    }

    [Fact]
    public void Serialize_EncryptsSensitiveValues()
    {
        var entries = new List<EnvEntry> { new("PWD", "secret", true) };
        var content = _svc.SerializeFile(entries);

        Assert.Contains("ENC:", content);
        Assert.DoesNotContain("secret", content);
    }

    [Fact]
    public void Serialize_WritesComments()
    {
        var entries = new List<EnvEntry> { new("FOO", "bar", false, "a comment") };
        var content = _svc.SerializeFile(entries);

        Assert.Contains("# a comment", content);
        Assert.Contains("FOO=bar", content);
    }
}
