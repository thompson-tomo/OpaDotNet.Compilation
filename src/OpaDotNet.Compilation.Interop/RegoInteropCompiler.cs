using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using OpaDotNet.Compilation.Abstractions;

namespace OpaDotNet.Compilation.Interop;

public class RegoInteropCompiler : IRegoCompiler
{
    private static IOptions<RegoCompilerOptions> Default { get; } = new OptionsWrapper<RegoCompilerOptions>(new());

    private readonly ILogger _logger;

    private readonly IOptions<RegoCompilerOptions> _options;

    public RegoInteropCompiler(
        IOptions<RegoCompilerOptions>? options = null,
        ILogger<RegoInteropCompiler>? logger = null)
    {
        _options = options ?? Default;
        _logger = logger ?? NullLogger<RegoInteropCompiler>.Instance;
    }

    public Task<RegoCompilerVersion> Version(CancellationToken cancellationToken = default)
    {
        var v = Interop.OpaGetVersion();

        var result = new RegoCompilerVersion
        {
            Version = v.LibVersion,
            Commit = v.Commit,
            Platform = v.Platform,
            GoVersion = v.GoVersion,
        };

        return Task.FromResult(result);
    }

    public Task<Stream> CompileBundle(
        string bundlePath,
        IEnumerable<string>? entrypoints = null,
        string? capabilitiesFilePath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(bundlePath);

        var result = Interop.Compile(
            bundlePath,
            true,
            _options.Value,
            entrypoints,
            capabilitiesFilePath,
            _logger
            );

        return Task.FromResult(result);
    }

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