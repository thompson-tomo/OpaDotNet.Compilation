using JetBrains.Annotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpaDotNet.Compilation.Cli;

using Xunit.Abstractions;

namespace OpaDotNet.Compilation.Tests;

[UsedImplicitly]
public class CliCompilerTests : CompilerTests<RegoCliCompiler, RegoCliCompilerOptions>
{
    public CliCompilerTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override RegoCliCompiler CreateCompiler(RegoCliCompilerOptions opts, ILoggerFactory loggerFactory)
    {
        return new RegoCliCompiler(
            new OptionsWrapper<RegoCliCompilerOptions>(opts),
            loggerFactory.CreateLogger<RegoCliCompiler>()
            );
    }
}