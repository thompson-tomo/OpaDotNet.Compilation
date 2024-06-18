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
        var vp = nint.Zero;

        try
        {
            Interop.OpaGetVersion(out vp);

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
        finally
        {
            if (vp != nint.Zero)
                Interop.OpaFreeVersion(vp);
        }

    }

    /// <inheritdoc />
    public async Task<Stream> Compile(
        string path,
        CompilationParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(parameters);

        if (path.StartsWith("./") || path.StartsWith(".\\"))
            path = path[2..];

        var caps = parameters.CapabilitiesStream;

        try
        {
            if (!string.IsNullOrWhiteSpace(parameters.CapabilitiesFilePath))
                caps = new FileStream(parameters.CapabilitiesFilePath, FileMode.Open);

            var result = Interop.Compile(
                NormalizePath(path),
                parameters.IsBundle,
                _options.Value,
                parameters.Entrypoints,
                caps,
                parameters.Revision,
                _logger
                );

            return result;
        }
        finally
        {
            if (caps != null)
                await caps.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<Stream> Compile(
        Stream stream,
        CompilationParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(parameters);

        var caps = parameters.CapabilitiesStream;
        Stream? bundle = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(parameters.CapabilitiesFilePath))
                caps = new FileStream(parameters.CapabilitiesFilePath, FileMode.Open);

            if (!parameters.IsBundle)
            {
                bundle = new MemoryStream();
                var bw = new BundleWriter(bundle);

                await using (bw.ConfigureAwait(false))
                    bw.WriteEntry(stream, "policy.rego");

                bundle.Seek(0, SeekOrigin.Begin);
            }

            var result = Interop.Compile(
                bundle ?? stream,
                true,
                _options.Value,
                parameters.Entrypoints,
                caps,
                parameters.Revision,
                _logger
                );

            return result;
        }
        finally
        {
            if (bundle != null)
                await bundle.DisposeAsync().ConfigureAwait(false);

            if (caps != null)
                await caps.DisposeAsync().ConfigureAwait(false);
        }
    }
}