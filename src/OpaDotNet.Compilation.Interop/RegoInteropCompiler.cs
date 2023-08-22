using System.Runtime.InteropServices;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using OpaDotNet.Compilation.Abstractions;

namespace OpaDotNet.Compilation.Interop;

/// <summary>
/// Compiles OPA bundle with OPA SDK interop wrapper.
/// </summary>
public class RegoInteropCompiler : IRegoCompiler
{
    private static IOptions<RegoCompilerOptions> Default { get; } = new OptionsWrapper<RegoCompilerOptions>(new());

    private readonly ILogger _logger;

    private readonly IOptions<RegoCompilerOptions> _options;

    /// <summary>
    /// Creates new instance of <see cref="RegoInteropCompiler"/> class.
    /// </summary>
    /// <param name="options">Compilation options</param>
    /// <param name="logger">Logger instance</param>
    public RegoInteropCompiler(
        IOptions<RegoCompilerOptions>? options = null,
        ILogger<RegoInteropCompiler>? logger = null)
    {
        _options = options ?? Default;
        _logger = logger ?? NullLogger<RegoInteropCompiler>.Instance;
    }

    private static string NormalizePath(string path) => path.Replace("\\", "/");

    /// <inheritdoc />
    public Task<RegoCompilerVersion> Version(CancellationToken cancellationToken = default)
    {
        var vp = Interop.OpaGetVersion();

        if (vp == nint.Zero)
            throw new RegoCompilationException("Failed to get version");

        var v = Marshal.PtrToStructure<Interop.OpaVersion>(vp);

        var result = new RegoCompilerVersion
        {
            Version = v.LibVersion,
            Commit = v.Commit,
            Platform = v.Platform,
            GoVersion = v.GoVersion,
        };

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Stream> CompileBundle(
        string bundlePath,
        IEnumerable<string>? entrypoints = null,
        string? capabilitiesFilePath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(bundlePath);

        var path = bundlePath;

        if (path.StartsWith("./") || path.StartsWith(".\\"))
            path = path[2..];

        Stream? caps = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(capabilitiesFilePath))
                caps = new FileStream(capabilitiesFilePath, FileMode.Open);

            var result = Interop.Compile(
                NormalizePath(path),
                true,
                _options.Value,
                entrypoints,
                caps,
                _logger
                );

            return Task.FromResult(result);
        }
        finally
        {
            caps?.Dispose();
        }
    }

    /// <inheritdoc />
    public Task<Stream> CompileFile(
        string sourceFilePath,
        IEnumerable<string>? entrypoints = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceFilePath);

        var path = sourceFilePath;

        if (path.StartsWith("./") || path.StartsWith(".\\"))
            path = path[2..];

        var result = Interop.Compile(
            NormalizePath(path),
            false,
            _options.Value,
            entrypoints,
            null,
            _logger
            );

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task<Stream> CompileSource(
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

    /// <inheritdoc />
    public Task<Stream> CompileStream(
        Stream bundle,
        IEnumerable<string>? entrypoints = null,
        Stream? capabilitiesJson = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        var result = Interop.Compile(
            bundle,
            true,
            _options.Value,
            entrypoints,
            capabilitiesJson,
            _logger
            );

        return Task.FromResult(result);
    }
}