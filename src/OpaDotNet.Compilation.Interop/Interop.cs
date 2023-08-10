using System.Runtime.InteropServices;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using OpaDotNet.Compilation.Abstractions;

namespace OpaDotNet.Compilation.Interop;

internal static class Interop
{
    private const string Lib = "Opa.Interop";

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OpaVersion
    {
        public string LibVersion;

        public string GoVersion;

        public string Commit;

        public string Platform;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct OpaBuildParams
    {
        public string Source;

        public string Target;

        public string? CapabilitiesFile;

        public string? CapabilitiesVersion;

        [MarshalAs(UnmanagedType.I1)] public bool BundleMode;

        public nint Entrypoints;

        public int EntrypointsLen;

        [MarshalAs(UnmanagedType.I1)] public bool Debug;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private unsafe struct OpaBuildResult
    {
        public byte* Result;

        public int ResultLen;

        public sbyte* Errors;

        public sbyte* Log;
    };

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern OpaVersion OpaGetVersion();

    [DllImport(Lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe int OpaBuildEx(
        ref OpaBuildParams buildParams,
        OpaBuildResult** buildResult);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void OpaFree(OpaBuildResult* buildResult);

    public static Stream Compile(
        string source,
        bool isBundle,
        RegoCompilerOptions options,
        IEnumerable<string>? entrypoints = null,
        string? capabilitiesFile = null,
        ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(source);
        ArgumentNullException.ThrowIfNull(options);

        logger ??= NullLogger.Instance;

        var pEntrypoints = nint.Zero;
        var entrypointsList = Array.Empty<nint>();

        try
        {
            var buildParams = new OpaBuildParams
            {
                Source = source,
                CapabilitiesVersion = options.CapabilitiesVersion,
                CapabilitiesFile = capabilitiesFile,
                BundleMode = isBundle,
                Target = "wasm",
                Debug = options.Debug,
            };

            if (entrypoints != null)
            {
                var ep = entrypoints as string[] ?? entrypoints.ToArray();
                pEntrypoints = Marshal.AllocCoTaskMem(ep.Length * nint.Size);
                entrypointsList = new nint[ep.Length];

                for (var i = 0; i < ep.Length; i++)
                    entrypointsList[i] = Marshal.StringToCoTaskMemAnsi(ep[i]);

                Marshal.Copy(entrypointsList, 0, pEntrypoints, ep.Length);

                buildParams.Entrypoints = pEntrypoints;
                buildParams.EntrypointsLen = ep.Length;
            }

            unsafe
            {
                OpaBuildResult* bundle = null;

                try
                {
                    var result = OpaBuildEx(ref buildParams, &bundle);

                    if (bundle->Log != null)
                    {
                        var message = new string(bundle->Log);
                        logger.LogDebug("{CompilationLog}", message);
                    }

                    if (result != 0)
                    {
                        var message = "Unknown compilation error";

                        if (bundle->Errors != null)
                            message = new string(bundle->Errors);

                        throw new RegoCompilationException(source, message);
                    }

                    if (bundle->Result == null || bundle->ResultLen <= 0)
                        throw new RegoCompilationException(source, "Bad result");

                    var buffer = new Span<byte>(bundle->Result, bundle->ResultLen);

                    return new MemoryStream(buffer.ToArray());
                }
                finally
                {
                    if (bundle != null)
                        OpaFree(bundle);
                }
            }
        }
        finally
        {
            if (pEntrypoints != nint.Zero)
            {
                foreach (var p in entrypointsList)
                    Marshal.FreeCoTaskMem(p);

                Marshal.FreeCoTaskMem(pEntrypoints);
            }
        }
    }
}