using System.Formats.Tar;
using System.IO.Compression;

namespace OpaDotNet.Compilation.Tests.Common;

public static class AssertPolicy
{
    public static void IsValid(Stream bundle, bool hasData = false)
    {
        var policy = TarGzHelper.ReadBundle(bundle);

        Assert.True(policy.Policy.Length > 0);

        if (hasData)
            Assert.True(policy.Data.Length > 0);
    }
}

internal record OpaPolicy(ReadOnlyMemory<byte> Policy, ReadOnlyMemory<byte> Data);

internal static class TarGzHelper
{
    public static OpaPolicy ReadBundle(Stream archive)
    {
        ArgumentNullException.ThrowIfNull(archive);

        using var gzip = new GZipStream(archive, CompressionMode.Decompress);
        using var ms = new MemoryStream();

        gzip.CopyTo(ms);
        ms.Seek(0, SeekOrigin.Begin);

        using var tr = new TarReader(ms);

        static Memory<byte> ReadEntry(TarEntry entry)
        {
            if (entry.DataStream == null)
                throw new InvalidOperationException($"Failed to read {entry.Name}");

            var result = new byte[entry.DataStream.Length];
            var bytesRead = entry.DataStream.Read(result);

            if (bytesRead < entry.DataStream.Length)
                throw new Exception($"Failed to read tar entry {entry.Name}");

            return result;
        }

        Memory<byte>? policy = null;
        Memory<byte>? data = null;

        while (tr.GetNextEntry() is { } entry)
        {
            if (string.Equals(entry.Name, "/policy.wasm", StringComparison.OrdinalIgnoreCase))
                policy = ReadEntry(entry);

            if (string.Equals(entry.Name, "/data.json", StringComparison.OrdinalIgnoreCase))
                data = ReadEntry(entry);
        }

        if (policy == null)
            throw new Exception("Bundle does not contain policy.wasm file");

        return new(policy.Value, data ?? Memory<byte>.Empty);
    }
}