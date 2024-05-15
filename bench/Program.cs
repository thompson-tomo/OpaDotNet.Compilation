using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

using Microsoft.Extensions.Options;
using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Cli;
using OpaDotNet.Compilation.Interop;
using System;
using System.IO;
using System.IO.Compression;
using System.Formats.Tar;
using System.Threading.Tasks;

//var config = DefaultConfig.Instance;
//var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

var SourcePath = Path.Combine("TestData", "policy.rego");
string[] _entrypoints = { "example/allow" };

//GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

var opts = new RegoCompilerOptions();
opts.Debug = true;
var fs = File.OpenRead(SourcePath);
var bb = new byte[fs.Length];
fs.Read(bb, 0, (int)fs.Length);

for (var i = 0; i < 1000; i++)
{
    var interop = new RegoInteropCompiler(new OptionsWrapper<RegoCompilerOptions>(opts));

    using var r = await interop.CompileBundle("TestData", _entrypoints);
    //DumpBundle(r);
    var v = await interop.Version();
    Console.WriteLine(v.Version);

    var bundle = TarGzHelper.ReadBundle(r);

    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
}

Console.WriteLine("Done!");
//Console.ReadLine();

static void DumpBundle(Stream bundle)
{
    ArgumentNullException.ThrowIfNull(bundle);

    using var gzip = new GZipStream(bundle, CompressionMode.Decompress, true);
    using var ms = new MemoryStream();

    gzip.CopyTo(ms);
    ms.Seek(0, SeekOrigin.Begin);

    using var tr = new TarReader(ms);

    while (tr.GetNextEntry() is { } entry)
        Console.WriteLine($"{entry.Name} [{entry.EntryType}] {entry.Length}");

    bundle.Seek(0, SeekOrigin.Begin);
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