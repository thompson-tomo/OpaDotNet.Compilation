using Microsoft.Extensions.Logging;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Tests.Common;

using Xunit.Abstractions;

namespace OpaDotNet.Compilation.Tests;

public abstract class CompilerTests<T, TOptions>
    where T : IRegoCompiler
    where TOptions : RegoCompilerOptions, new()
{
    private readonly ILoggerFactory _loggerFactory;

    protected CompilerTests(ITestOutputHelper output)
    {
        _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
    }

    protected abstract T CreateCompiler(TOptions opts, ILoggerFactory loggerFactory);

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
        var compiler = CreateCompiler(opts, _loggerFactory);
        var policy = await compiler.CompileFile(Path.Combine("TestData", "policy.rego"), eps);

        AssertPolicy.IsValid(policy);
    }

    [Fact]
    public async Task FailCompilation()
    {
        var opts = new TOptions();
        var compiler = CreateCompiler(opts, _loggerFactory);
        var ex = await Assert.ThrowsAsync<RegoCompilationException>(() => compiler.CompileSource("bad rego", new[] { "ep" }));

        Assert.Contains("rego_parse_error: package expected", ex.Message);
    }

    [Fact]
    public async Task MergeCapabilities()
    {
        var opts = new TOptions
        {
            CapabilitiesVersion = "v0.53.1",
        };

        var compiler = CreateCompiler(opts, _loggerFactory);

        await using var policy = await compiler.CompileBundle(
            Path.Combine("TestData", "capabilities"),
            new[] { "capabilities/f" },
            Path.Combine("TestData", "capabilities", "capabilities.json")
            );

        AssertPolicy.IsValid(policy);
    }
}