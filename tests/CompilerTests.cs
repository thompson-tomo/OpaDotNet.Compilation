using System.Text;
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
    [InlineData(null, "./TestData/policy.rego")]
    [InlineData(null, "TestData/policy.rego")]
    [InlineData(null, ".\\TestData\\policy.rego")]
    [InlineData(null, "TestData\\policy.rego")]
    [InlineData(null, "~TestData\\policy.rego")]
    public async Task CompileFile(string? entrypoint, string? path = null)
    {
        var opts = new TOptions
        {
            Debug = true,
        };

        var eps = string.IsNullOrWhiteSpace(entrypoint) ? null : new[] { entrypoint };
        var compiler = CreateCompiler(opts, LoggerFactory);

        path ??= Path.Combine("TestData", "policy.rego");

        if (path.StartsWith("~"))
            path = Path.Combine(AppContext.BaseDirectory, path[1..]);

        var policy = await compiler.CompileFile(path, eps);

        AssertBundle.DumpBundle(policy, OutputHelper);

        AssertBundle.IsValid(policy);
    }

    [Theory]
    [InlineData("test1/hello")]
    [InlineData("test2/hello")]
    [InlineData("test1/hello", "./TestData/compile-bundle/example")]
    [InlineData("test1/hello", "TestData/compile-bundle/example")]
    [InlineData("test1/hello", ".\\TestData\\compile-bundle\\example")]
    [InlineData("test1/hello", "TestData\\compile-bundle\\example")]
    [InlineData("test1/hello", "~TestData\\compile-bundle\\example")]
    public async Task CompileBundle(string? entrypoint, string? path = null)
    {
        var opts = new TOptions
        {
            Debug = true,
        };

        var eps = string.IsNullOrWhiteSpace(entrypoint) ? null : new[] { entrypoint };
        var compiler = CreateCompiler(opts, LoggerFactory);

        path ??= Path.Combine("TestData", "compile-bundle", "example");

        if (path.StartsWith("~"))
            path = Path.Combine(AppContext.BaseDirectory, path[1..]);

        var policy = await compiler.CompileBundle(path, eps);

        AssertBundle.DumpBundle(policy, OutputHelper);

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

        OutputHelper.WriteLine(v.ToString());
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
    public async Task BundleWriterMergeCapabilities()
    {
        var opts = new TOptions
        {
            CapabilitiesVersion = "v0.53.1",
            Debug = true,
        };

        using var bundle = new MemoryStream();

        await using (var bw = new BundleWriter(bundle))
        {
            var rego = await File.ReadAllBytesAsync(Path.Combine("TestData", "capabilities", "capabilities.rego"));
            bw.WriteEntry(rego, "capabilities.rego");
        }

        bundle.Seek(0, SeekOrigin.Begin);
        await using var capsFs = File.OpenRead(Path.Combine("TestData", "capabilities", "capabilities.json"));

        var compiler = CreateCompiler(opts, LoggerFactory);

        await using var policy = await compiler.CompileStream(
            bundle,
            new[] { "capabilities/f" },
            capsFs
            );

        AssertBundle.DumpBundle(policy, OutputHelper);

        AssertBundle.Content(
            policy,
            p => AssertBundle.HasEntry(p, "/policy.wasm")
            );
    }

    [Fact]
    public async Task BundleWriterFile()
    {
        using var ms = new MemoryStream();

        var manifest = new BundleManifest
        {
            Revision = "test-2",
            Metadata = { { "source", "test" } },
        };

        await using (var bw = new BundleWriter(ms, manifest))
        {
            using var inStream = new MemoryStream();
            inStream.Write(Encoding.UTF8.GetBytes(TestHelpers.PolicySource("p2", "p2r")));
            inStream.Seek(0, SeekOrigin.Begin);

            bw.WriteEntry(TestHelpers.SimplePolicySource, "p1.rego");
            bw.WriteEntry(inStream, "/tests/p2.rego");
            bw.WriteEntry("{}"u8, "/tests/data.json");
            bw.WriteEntry("{}"u8, @"c:\a\data.json");
        }

        ms.Seek(0, SeekOrigin.Begin);

        var opts = new TOptions
        {
            PruneUnused = true,
            Debug = true,
            OutputPath = "./tmp",
        };

        var tmpDir = new DirectoryInfo(opts.OutputPath);

        if (!tmpDir.Exists)
            tmpDir.Create();

        var compiler = CreateCompiler(opts, LoggerFactory);
        var bundle = await compiler.CompileStream(ms, TestHelpers.SimplePolicyEntrypoints);

        AssertBundle.DumpBundle(bundle, OutputHelper);

        AssertBundle.Content(
            bundle,
            p => AssertBundle.HasEntry(p, "/policy.wasm"),
            p => AssertBundle.HasEntry(p, "/.manifest"),
            AssertBundle.HasNonEmptyData
            );
    }

    [Fact]
    public async Task EnsureCleanup()
    {
        using var ms = new MemoryStream();

        await using (var bw = new BundleWriter(ms))
        {
            using var inStream = new MemoryStream();
            inStream.Write(Encoding.UTF8.GetBytes(TestHelpers.PolicySource("p2", "p2r")));
            inStream.Seek(0, SeekOrigin.Begin);

            bw.WriteEntry(TestHelpers.SimplePolicySource, "p1.rego");
            bw.WriteEntry(inStream, "/tests/p2.rego");
            bw.WriteEntry("{}"u8, "/tests/data.json");
            bw.WriteEntry("{}"u8, @"c:\a\data.json");
        }

        ms.Seek(0, SeekOrigin.Begin);

        var opts = new TOptions
        {
            PruneUnused = true,
            Debug = true,
            OutputPath = "./tmp-cleanup",
        };

        var tmpDir = new DirectoryInfo(opts.OutputPath);

        if (tmpDir.Exists)
            tmpDir.Delete(true);

        tmpDir.Create();

        var compiler = CreateCompiler(opts, LoggerFactory);
        var bundle = await compiler.CompileStream(ms, TestHelpers.SimplePolicyEntrypoints);

        await bundle.DisposeAsync();

        var filesCount = tmpDir.EnumerateFiles().Count();
        Assert.Equal(0, filesCount);
    }

    [Fact]
    public async Task EnsureCleanupOnError()
    {
        using var ms = new MemoryStream();

        await using (var bw = new BundleWriter(ms))
        {
            using var inStream = new MemoryStream();
            inStream.Write("bad policy"u8);
            inStream.Seek(0, SeekOrigin.Begin);

            bw.WriteEntry(inStream, "/tests/p2.rego");
        }

        ms.Seek(0, SeekOrigin.Begin);

        var opts = new TOptions
        {
            PruneUnused = true,
            Debug = true,
            OutputPath = "./tmp-cleanup-fail",
        };

        var tmpDir = new DirectoryInfo(opts.OutputPath);

        if (tmpDir.Exists)
            tmpDir.Delete(true);

        tmpDir.Create();

        var compiler = CreateCompiler(opts, LoggerFactory);
        await Assert.ThrowsAsync<RegoCompilationException>(() => compiler.CompileStream(ms, TestHelpers.SimplePolicyEntrypoints));

        var filesCount = tmpDir.EnumerateFiles().Count();
        Assert.Equal(0, filesCount);
    }
}