using System.Text.Json;

using Microsoft.Extensions.Logging;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Tests.Common;

using Xunit.Abstractions;

namespace OpaDotNet.Compilation.Tests;

public abstract class CompilerTests<T, TOptions>
    where T : IRegoCompiler
    where TOptions : RegoCompilerOptions, new()
{
    protected readonly ILoggerFactory LoggerFactory;

    protected readonly ITestOutputHelper OutputHelper;

    protected CompilerTests(ITestOutputHelper output)
    {
        OutputHelper = output;
        LoggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
    }

    protected abstract T CreateCompiler(TOptions? opts = null, ILoggerFactory? loggerFactory = null);

    [Theory]
    [InlineData("ex/test")]
    [InlineData(null)]
    public async Task CompileFile(string? entrypoint)
    {
        var opts = new TOptions
        {
            Debug = true,
        };

        var eps = string.IsNullOrWhiteSpace(entrypoint) ? null : new[] { entrypoint };
        var compiler = CreateCompiler(opts, LoggerFactory);
        var policy = await compiler.CompileFile(Path.Combine("TestData", "policy.rego"), eps);

        AssertBundle.IsValid(policy);
    }

    [Theory]
    [InlineData("test1/hello")]
    [InlineData("test2/hello")]
    [InlineData("test1/hello", "./TestData/compile-bundle/example")]
    public async Task CompileBundle(string? entrypoint, string? path = null)
    {
        var opts = new TOptions
        {
            Debug = true,
        };

        var eps = string.IsNullOrWhiteSpace(entrypoint) ? null : new[] { entrypoint };
        var compiler = CreateCompiler(opts, LoggerFactory);

        path ??= Path.Combine("TestData", "compile-bundle", "example");
        var policy = await compiler.CompileBundle(path, eps);

        var bundle = TarGzHelper.ReadBundle(policy);

        Assert.True(bundle.Policy.Length > 0);
        Assert.True(bundle.Data.Length > 0);

        var data = JsonDocument.Parse(bundle.Data);

        Assert.Equal("root", data.RootElement.GetProperty("root").GetProperty("world").GetString());
        Assert.Equal("world", data.RootElement.GetProperty("test1").GetProperty("world").GetString());
        Assert.Equal("world1", data.RootElement.GetProperty("test2").GetProperty("world").GetString());
    }

    [Theory]
    [InlineData("test1/hello")]
    [InlineData("test2/hello")]
    [InlineData("test1/hello", "./TestData/src.bundle.tar.gz")]
    public async Task CompileBundleFromBundle(string? entrypoint, string? path = null)
    {
        var opts = new TOptions
        {
            Debug = true,
        };

        var eps = string.IsNullOrWhiteSpace(entrypoint) ? null : new[] { entrypoint };
        var compiler = CreateCompiler(opts, LoggerFactory);

        path ??= Path.Combine("TestData", "src.bundle.tar.gz");
        var policy = await compiler.CompileBundle(path, eps);

        AssertBundle.IsValid(policy);
    }

    [Fact]
    public async Task Version()
    {
        var compiler = CreateCompiler();
        var v = await compiler.Version();

        Assert.NotNull(v.Version);
        Assert.NotNull(v.GoVersion);
        Assert.NotNull(v.Platform);
    }

    [Fact]
    public async Task FailCompilation()
    {
        var opts = new TOptions();
        var compiler = CreateCompiler(opts, LoggerFactory);
        var ex = await Assert.ThrowsAsync<RegoCompilationException>(() => compiler.CompileSource("bad rego", new[] { "ep" }));

        Assert.Contains("rego_parse_error: package expected", ex.Message);
    }

    [Fact]
    public async Task FailCapabilities()
    {
        var compiler = CreateCompiler(new(), LoggerFactory);

        _ = await Assert.ThrowsAsync<RegoCompilationException>(
            () => compiler.CompileBundle(
                Path.Combine("TestData", "capabilities"),
                new[] { "capabilities/f" },
                Path.Combine("TestData", "capabilities", "capabilities.json")
                )
            );
    }

    [Fact]
    public async Task SetCapabilitiesBundle()
    {
        var opts = new TOptions
        {
            CapabilitiesVersion = "v0.53.1",
        };

        var compiler = CreateCompiler(opts, LoggerFactory);

        await using var policy = await compiler.CompileBundle(
            Path.Combine("TestData", "compile-bundle", "example"),
            new[] { "test1/hello", "test2/hello" }
            );

        Assert.NotNull(policy);
    }

    [Fact]
    public async Task SetCapabilitiesSource()
    {
        var opts = new TOptions
        {
            CapabilitiesVersion = "v0.53.1",
        };

        var compiler = CreateCompiler(opts, LoggerFactory);

        await using var policy = await compiler.CompileSource(
            TestHelpers.SimplePolicySource,
            TestHelpers.SimplePolicyEntrypoints
            );

        Assert.NotNull(policy);
    }

    [Fact]
    public async Task MergeCapabilities()
    {
        var opts = new TOptions
        {
            CapabilitiesVersion = "v0.53.1",
        };

        var compiler = CreateCompiler(opts, LoggerFactory);

        await using var policy = await compiler.CompileBundle(
            Path.Combine("TestData", "capabilities"),
            new[] { "capabilities/f" },
            Path.Combine("TestData", "capabilities", "capabilities.json")
            );

        AssertBundle.IsValid(policy);
    }

    [Fact]
    public async Task BundleWriterFile()
    {
        using var ms = new MemoryStream();

        await using (var bw = new BundleWriter(ms))
        {
            bw.WriteEntry(TestHelpers.SimplePolicySource, "p1.rego");
            bw.WriteEntry(TestHelpers.PolicySource("p2", "p2r"), "/tests/p2.rego");
            bw.WriteEntry("{}", "/tests/data.json");
        }

        ms.Seek(0, SeekOrigin.Begin);

        await File.WriteAllBytesAsync("policy.tar.gz", ms.ToArray());

        var compiler = CreateCompiler();
        var bundle = await compiler.CompileBundle("policy.tar.gz", TestHelpers.SimplePolicyEntrypoints);

        AssertBundle.DumpBundle(bundle, OutputHelper);

        AssertBundle.Content(
            bundle,
            p => AssertBundle.HasEntry(p, "/policy.wasm"),
            AssertBundle.HasNonEmptyData
            );
    }
}