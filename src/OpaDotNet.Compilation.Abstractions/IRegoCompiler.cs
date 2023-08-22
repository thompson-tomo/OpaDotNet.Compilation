using JetBrains.Annotations;

namespace OpaDotNet.Compilation.Abstractions;

/// <summary>
/// Exposes an OPA policy compiler.
/// </summary>
[PublicAPI]
public interface IRegoCompiler
{
    /// <summary>
    /// Compiler version information.
    /// </summary>
    public Task<RegoCompilerVersion> Version(CancellationToken cancellationToken = default);

    /// <summary>
    /// Compiles OPA bundle from bundle directory.
    /// </summary>
    /// <param name="bundlePath">Bundle directory or bundle archive path.</param>
    /// <param name="entrypoints">Which documents (entrypoints) will be queried when asking for policy decisions.</param>
    /// <param name="capabilitiesFilePath">
    /// Capabilities file that defines the built-in functions and other language features that policies may depend on.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    Task<Stream> CompileBundle(
        string bundlePath,
        IEnumerable<string>? entrypoints = null,
        string? capabilitiesFilePath = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compiles OPA bundle from rego policy source file.
    /// </summary>
    /// <param name="sourceFilePath">Source file path.</param>
    /// <param name="entrypoints">Which documents (entrypoints) will be queried when asking for policy decisions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    Task<Stream> CompileFile(
        string sourceFilePath,
        IEnumerable<string>? entrypoints = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compiles OPA bundle from rego bundle stream.
    /// </summary>
    /// <param name="bundle">Rego bundle stream.</param>
    /// <param name="entrypoints">Which documents (entrypoints) will be queried when asking for policy decisions.</param>
    /// <param name="capabilitiesJson">
    /// Capabilities json that defines the built-in functions and other language features that policies may depend on.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    Task<Stream> CompileStream(
        Stream bundle,
        IEnumerable<string>? entrypoints = null,
        Stream? capabilitiesJson = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compiles OPA bundle from rego policy source code.
    /// </summary>
    /// <param name="source">Source file path.</param>
    /// <param name="entrypoints">Which documents (entrypoints) will be queried when asking for policy decisions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    async Task<Stream> CompileSource(
        string source,
        IEnumerable<string>? entrypoints = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(source);

        using var bundle = new MemoryStream();
        var bw = new BundleWriter(bundle);

        await using (bw.ConfigureAwait(false))
            bw.WriteEntry(source, "policy.rego");

        bundle.Seek(0, SeekOrigin.Begin);

        return await CompileStream(bundle, entrypoints, null, cancellationToken).ConfigureAwait(false);
    }
}