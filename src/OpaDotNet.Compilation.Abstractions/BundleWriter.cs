using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

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
    /// <param name="manifest">Policy bundle manifest.</param>
    public BundleWriter(Stream stream, BundleManifest? manifest = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var zip = new GZipStream(stream, CompressionMode.Compress, true);
        _writer = new TarWriter(zip);

        if (manifest != null)
            WriteEntry(JsonSerializer.Serialize(manifest), ".manifest");
    }

    private static string NormalizePath(string path) => "/" + path.Replace("\\", "/").TrimStart('/');

    /// <summary>
    /// Merges two capabilities.json streams.
    /// </summary>
    /// <param name="caps1">First capabilities.json stream.</param>
    /// <param name="caps2">Second capabilities.json stream.</param>
    /// <returns>Merged capabilities.json stream.</returns>
    public static Stream MergeCapabilities(Stream caps1, Stream caps2)
    {
        ArgumentNullException.ThrowIfNull(caps1);
        ArgumentNullException.ThrowIfNull(caps2);

        var resultDoc = JsonNode.Parse(caps1);

        if (resultDoc == null)
            throw new RegoCompilationException("Failed to parse capabilities file");

        var resultBins = resultDoc.Root["builtins"]?.AsArray();

        if (resultBins == null)
            throw new RegoCompilationException("Invalid capabilities file: 'builtins' node not found");

        var capsDoc = JsonDocument.Parse(caps2);
        var capsBins = capsDoc.RootElement.GetProperty("builtins");

        foreach (var bin in capsBins.EnumerateArray())
            resultBins.Add(bin);

        var ms = new MemoryStream();

        using (var jw = new Utf8JsonWriter(ms))
        {
            resultDoc.WriteTo(jw);
        }

        ms.Seek(0, SeekOrigin.Begin);

        return ms;
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

        if (Path.IsPathRooted(path))
        {
            if (Path.GetPathRoot(path)?[0] != Path.DirectorySeparatorChar)
                path = path[2..];
        }

        var entry = new PaxTarEntry(TarEntryType.RegularFile, NormalizePath(path))
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
        await _writer.DisposeAsync().ConfigureAwait(false);
    }
}