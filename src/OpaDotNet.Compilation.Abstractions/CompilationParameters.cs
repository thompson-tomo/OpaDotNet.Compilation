namespace OpaDotNet.Compilation.Abstractions;

/// <summary>
/// Compilation parameters.
/// </summary>
public record CompilationParameters
{
    /// <summary>
    /// Specifies if compilation source if file or bundle.
    /// </summary>
    public bool IsBundle { get; init; }

    /// <summary>
    /// Which documents (entrypoints) will be queried when asking for policy decisions.
    /// </summary>
    public IReadOnlySet<string>? Entrypoints { get; init; }

    /// <summary>
    /// Capabilities file that defines the built-in functions and other language features that policies may depend on.
    /// If <see cref="CapabilitiesFilePath"/> is specified <see cref="CapabilitiesStream"/> is ignored.
    /// </summary>
    public string? CapabilitiesFilePath { get; init; }

    /// <summary>
    /// Capabilities json that defines the built-in functions and other language features that policies may depend on.
    /// </summary>
    public Stream? CapabilitiesStream { get; init; }
}