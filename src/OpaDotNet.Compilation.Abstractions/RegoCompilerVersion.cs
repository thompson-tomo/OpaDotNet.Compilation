using JetBrains.Annotations;

namespace OpaDotNet.Compilation.Abstractions;

[PublicAPI]
public record RegoCompilerVersion
{
    public string? Version { get; set; }

    public string? Commit { get; set; }

    public string? GoVersion { get; set; }

    public string? Platform { get; set; }
}