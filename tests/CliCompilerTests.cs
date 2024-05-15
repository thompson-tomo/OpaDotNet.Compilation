using JetBrains.Annotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Cli;

using Xunit.Abstractions;

namespace OpaDotNet.Compilation.Tests;

[UsedImplicitly]
[Trait("NeedsCli", "true")]
[Trait("Category", "Cli")]
public class CliCompilerTests : CompilerTests<RegoCliCompiler, RegoCliCompilerOptions>
{
    public CliCompilerTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override string BaseOutputPath => "cli";

    protected override RegoCliCompiler CreateCompiler(RegoCliCompilerOptions? opts = null, ILoggerFactory? loggerFactory = null)
    {
        return new RegoCliCompiler(
            opts == null ? null : new OptionsWrapper<RegoCliCompilerOptions>(opts),
            loggerFactory?.CreateLogger<RegoCliCompiler>()
            );
    }

    [Fact]
    public async Task OpaCliNotFound()
    {
        var opts = new RegoCliCompilerOptions
        {
            OpaToolPath = "./somewhere",
            ExtraArguments = "--debug",
        };

        var compiler = new RegoCliCompiler(new OptionsWrapper<RegoCliCompilerOptions>(opts));

        _ = await Assert.ThrowsAsync<RegoCompilationException>(
            () => compiler.CompileFile("fail.rego")
            );
    }

    [Fact]
    public async Task PreserveBuildArtifacts()
    {
        var di = new DirectoryInfo("buildArtifacts");

        if (di.Exists)
            di.Delete(true);

        di.Create();

        var opts = new RegoCliCompilerOptions
        {
            CapabilitiesVersion = "v0.53.1",
            PreserveBuildArtifacts = true,
            OutputPath = di.FullName,
        };

        var compiler = new RegoCliCompiler(
            new OptionsWrapper<RegoCliCompilerOptions>(opts),
            LoggerFactory.CreateLogger<RegoCliCompiler>()
            );

        var policy = await compiler.CompileBundle(
            Path.Combine("TestData", "capabilities"),
            new[] { "capabilities/f" },
            Path.Combine("TestData", "capabilities", "capabilities.json")
            );

        Assert.IsType<FileStream>(policy);

        await policy.DisposeAsync();

        Assert.True(Directory.Exists(di.FullName));

        var files = Directory.GetFiles(di.FullName);

        Assert.Equal(2, files.Length);
        Assert.Contains(files, p => p.EndsWith("tar.gz"));
        Assert.Contains(files, p => p.EndsWith("json"));
    }
}