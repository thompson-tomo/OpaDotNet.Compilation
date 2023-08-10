using JetBrains.Annotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Interop;

using Xunit.Abstractions;

namespace OpaDotNet.Compilation.Tests;

[UsedImplicitly]
public class InteropCompilerTests : CompilerTests<RegoInteropCompiler, RegoCompilerOptions>
{
    public InteropCompilerTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override RegoInteropCompiler CreateCompiler(RegoCompilerOptions opts, ILoggerFactory loggerFactory)
    {
        return new RegoInteropCompiler(
            new OptionsWrapper<RegoCompilerOptions>(opts),
            loggerFactory.CreateLogger<RegoInteropCompiler>()
            );
    }
}