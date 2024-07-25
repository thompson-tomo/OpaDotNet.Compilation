using System.IO;
using System.Linq;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Cli;
using OpaDotNet.Compilation.Interop;

namespace OpaDotNet.Compilation.Benchmarks;

public class Compilers
{
    private static string SourcePath => Path.Combine("TestData", "policy.rego");

    private readonly string[] _entrypoints = { "example/allow" };

    [Benchmark]
    public async Task<Stream> Cli()
    {
        var cli = new RegoCliCompiler();
        return await cli.CompileFileAsync(SourcePath, new() { Entrypoints = _entrypoints?.ToHashSet() });
    }

    [Benchmark]
    public async Task<Stream> Interop()
    {
        var interop = new RegoInteropCompiler();
        return await interop.CompileFileAsync(SourcePath, new() { Entrypoints = _entrypoints?.ToHashSet() });
    }
}