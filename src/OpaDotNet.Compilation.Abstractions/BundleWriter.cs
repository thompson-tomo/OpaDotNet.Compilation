using System.Formats.Tar;
using System.IO.Compression;
using System.Text;

using JetBrains.Annotations;

namespace OpaDotNet.Compilation.Abstractions;

/// <summary>
/// Implements writing files into OPA policy bundle.
/// </summary>
/// <remarks>You need to dispose <see cref="BundleWriter"/> instance before you can use resulting bundle.</remarks>
/// <example>
/// <code>
/// using var ms = new MemoryStream();
///
/// using (var writer = new BundleWriter(ms))
/// {
///     writer.WriteEntry("package test", "policy.rego");
/// }
///
/// // Now bundle have been constructed.
/// ms.Seek(0, SeekOrigin.Begin);
/// ...
/// </code>
/// </example>
[PublicAPI]
public sealed class BundleWriter : IDisposable, IAsyncDisposable
{
    private readonly TarWriter _writer;

    /// <summary>
    /// Creates new instance of <see cref="BundleWriter"/>.
    /// </summary>
    /// <param name="stream">Stream to write bundle to.</param>
    public BundleWriter(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var zip = new GZipStream(stream, CompressionMode.Compress, true);
        _writer = new TarWriter(zip);
    }

    /// <summary>
    /// Writes string content into bundle.
    /// </summary>
    /// <param name="str">String content.</param>
    /// <param name="path">Relative file path inside bundle.</param>
    public void WriteEntry(ReadOnlySpan<char> str, string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        Span<byte> bytes = new byte[Encoding.UTF8.GetByteCount(str)];
        _ = Encoding.UTF8.GetBytes(str, bytes);

        WriteEntry(bytes, path);
    }

    /// <summary>
    /// Writes bytes content into bundle.
    /// </summary>
    /// <param name="bytes">String content.</param>
    /// <param name="path">Relative file path inside bundle.</param>
    public void WriteEntry(ReadOnlySpan<byte> bytes, string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        using var ms = new MemoryStream(bytes.Length);
        ms.Write(bytes);
        ms.Seek(0, SeekOrigin.Begin);

        WriteEntry(ms, path);
    }

    /// <summary>
    /// Writes stream content into bundle.
    /// </summary>
    /// <param name="stream">String content.</param>
    /// <param name="path">Relative file path inside bundle.</param>
    public void WriteEntry(Stream stream, string path)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrEmpty(path);

        var normPath = "/" + path.Replace("\\", "/").TrimStart('/');

        var entry = new PaxTarEntry(TarEntryType.RegularFile, normPath)
        {
            DataStream = stream,
        };

        _writer.WriteEntry(entry);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _writer.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _writer.DisposeAsync();
    }
}