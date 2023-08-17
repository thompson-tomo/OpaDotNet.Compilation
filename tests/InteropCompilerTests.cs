using System.Reflection;
using System.Runtime.InteropServices;

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
    static InteropCompilerTests()
    {
        // https://github.com/dotnet/sdk/issues/24708
        NativeLibrary.SetDllImportResolver(
            typeof(RegoInteropCompiler).Assembly,
            DllImportResolver
            );
    }

    public InteropCompilerTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override RegoInteropCompiler CreateCompiler(RegoCompilerOptions? opts = null, ILoggerFactory? loggerFactory = null)
    {
        return new RegoInteropCompiler(
            opts == null ? null : new OptionsWrapper<RegoCompilerOptions>(opts),
            loggerFactory?.CreateLogger<RegoInteropCompiler>()
            );
    }

    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return NativeLibrary.Load(Path.Combine("runtimes/linux-x64/native", libraryName), assembly, searchPath);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return NativeLibrary.Load(Path.Combine("runtimes/win-x64/native", libraryName), assembly, searchPath);

        return IntPtr.Zero;
    }
}