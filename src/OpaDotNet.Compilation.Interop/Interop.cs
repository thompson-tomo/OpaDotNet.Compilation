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
        [MarshalAs(UnmanagedType.LPStr)]
        public string LibVersion;

        [MarshalAs(UnmanagedType.LPStr)]
        public string GoVersion;

        [MarshalAs(UnmanagedType.LPStr)]
        public string Commit;

        [MarshalAs(UnmanagedType.LPStr)]
        public string Platform;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct OpaFsBuildParams
    {
        public string Source;

        public OpaBuildParams Params;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct OpaBuildParams
    {
        public string Target;

        public string? CapabilitiesFile;

        public string? CapabilitiesVersion;

        [MarshalAs(UnmanagedType.I1)]
        public bool BundleMode;

        public nint Entrypoints;

        public int EntrypointsLen;

        [MarshalAs(UnmanagedType.I1)]
        public bool Debug;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct OpaBuildResult
    {
        public nint Result;

        public int ResultLen;

        [MarshalAs(UnmanagedType.LPStr)]
        public string Errors;

        [MarshalAs(UnmanagedType.LPStr)]
        public string Log;
    }

    [DllImport(Lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint OpaGetVersion();

    [DllImport(Lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int OpaBuildFromFs(
        [In] ref OpaFsBuildParams buildParams,
        out nint buildResult);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void OpaFree(nint buildResult);

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

            var bundle = nint.Zero;

            try
            {
                var fsBuildParams = new OpaFsBuildParams
                {
                    Source = source,
                    Params = buildParams,
                };

                var result = OpaBuildFromFs(ref fsBuildParams, out bundle);

                if (bundle == nint.Zero)
                    throw new RegoCompilationException(source, "Compilation failed");

                var resultBundle = Marshal.PtrToStructure<OpaBuildResult>(bundle);

                if (!string.IsNullOrWhiteSpace(resultBundle.Log))
                    logger.LogDebug("{BuildLog}", resultBundle.Log);

                if (!string.IsNullOrWhiteSpace(resultBundle.Errors))
                    throw new RegoCompilationException(source, resultBundle.Errors);

                if (result != 0)
                    throw new RegoCompilationException(source, "Unknown compilation error");

                if (resultBundle.ResultLen == 0 || resultBundle.Result == nint.Zero)
                    throw new RegoCompilationException(source, "Bad result");

                var bundleBytes = new byte[resultBundle.ResultLen];
                Marshal.Copy(resultBundle.Result, bundleBytes, 0, resultBundle.ResultLen);

                return new MemoryStream(bundleBytes);
            }
            finally
            {
                if (bundle != nint.Zero)
                    OpaFree(bundle);
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