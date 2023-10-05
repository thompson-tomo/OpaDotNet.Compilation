namespace OpaDotNet.Compilation.Abstractions;

/// <summary>
/// Compilation extensions.
/// </summary>
public static class RegoCompilerExtensions
{
    /// <summary>
    /// Compiles OPA bundle from bundle directory.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="bundlePath">Bundle directory or bundle archive path.</param>
    /// <param name="entrypoints">Which documents (entrypoints) will be queried when asking for policy decisions.</param>
    /// <param name="capabilitiesFilePath">
    /// Capabilities file that defines the built-in functions and other language features that policies may depend on.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    public static Task<Stream> CompileBundle(
        this IRegoCompiler compiler,
        string bundlePath,
        IEnumerable<string>? entrypoints = null,
        string? capabilitiesFilePath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(bundlePath);

        var p = new CompilationParameters
        {
            IsBundle = true,
            Entrypoints = entrypoints?.ToHashSet(),
            CapabilitiesFilePath = capabilitiesFilePath,
        };

        return compiler.Compile(bundlePath, p, cancellationToken);
    }

    /// <summary>
    /// Compiles OPA bundle from rego policy source file.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="sourceFilePath">Source file path.</param>
    /// <param name="entrypoints">Which documents (entrypoints) will be queried when asking for policy decisions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    public static Task<Stream> CompileFile(
        this IRegoCompiler compiler,
        string sourceFilePath,
        IEnumerable<string>? entrypoints = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceFilePath);

        var p = new CompilationParameters
        {
            IsBundle = false,
            Entrypoints = entrypoints?.ToHashSet(),
        };

        return compiler.Compile(sourceFilePath, p, cancellationToken);
    }

    /// <summary>
    /// Compiles OPA bundle from rego bundle stream.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="bundle">Rego bundle stream.</param>
    /// <param name="entrypoints">Which documents (entrypoints) will be queried when asking for policy decisions.</param>
    /// <param name="capabilitiesJson">
    /// Capabilities json that defines the built-in functions and other language features that policies may depend on.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    public static Task<Stream> CompileStream(
        this IRegoCompiler compiler,
        Stream bundle,
        IEnumerable<string>? entrypoints = null,
        Stream? capabilitiesJson = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        var p = new CompilationParameters
        {
            IsBundle = true,
            Entrypoints = entrypoints?.ToHashSet(),
            CapabilitiesStream = capabilitiesJson,
        };

        return compiler.Compile(bundle, p, cancellationToken);
    }

    /// <summary>
    /// Compiles OPA bundle from rego policy source code.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="source">Source file path.</param>
    /// <param name="entrypoints">Which documents (entrypoints) will be queried when asking for policy decisions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    public static async Task<Stream> CompileSource(
        this IRegoCompiler compiler,
        string source,
        IEnumerable<string>? entrypoints = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(source);

        using var bundle = new MemoryStream();
        var bw = new BundleWriter(bundle);

        await using (bw.ConfigureAwait(false))
        {
            bw.WriteEntry(source, "policy.rego");
        }

        bundle.Seek(0, SeekOrigin.Begin);

        var p = new CompilationParameters
        {
            IsBundle = true,
            Entrypoints = entrypoints?.ToHashSet(),
        };

        return await compiler.Compile(bundle, p, cancellationToken).ConfigureAwait(false);
    }
}