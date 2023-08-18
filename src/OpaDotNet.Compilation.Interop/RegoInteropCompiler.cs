using System.Runtime.InteropServices;
using System.Text;

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

        var bp = new DirectoryInfo(bundlePath);

        var result = Interop.Compile(
            bp.FullName,
            true,
            _options.Value,
            entrypoints,
            capabilitiesFilePath,
            _logger
            );

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Stream> CompileFile(
        string sourceFilePath,
        IEnumerable<string>? entrypoints = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceFilePath);

        var result = Interop.Compile(
            sourceFilePath,
            false,
            _options.Value,
            entrypoints,
            logger: _logger
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

        var path = _options.Value.OutputPath ?? AppContext.BaseDirectory;
        var sourceFile = new FileInfo(Path.Combine(path, $"{Guid.NewGuid()}.rego"));
        await File.WriteAllTextAsync(sourceFile.FullName, source, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

        try
        {
            return await CompileFile(sourceFile.FullName, entrypoints, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (!_options.Value.PreserveBuildArtifacts)
                File.Delete(sourceFile.FullName);
        }
    }
}