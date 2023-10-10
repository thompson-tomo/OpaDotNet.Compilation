using JetBrains.Annotations;

namespace OpaDotNet.Compilation.Abstractions;

/// <summary>
/// Contains members that affect compiler behavior.
/// </summary>
[PublicAPI]
public class RegoCompilerOptions
{
    /// <summary>
    /// Path compiler will use to store intermediate compilation artifacts.
    /// </summary>
    /// <remarks>
    /// Directory must exist and requires write permissions.
    /// </remarks>
    public string? OutputPath { get; set; }

    /// <summary>
    /// OPA capabilities version. If set, compiler will merge capabilities
    /// of specified version with any additional custom capabilities.
    /// </summary>
    public string? CapabilitiesVersion { get; set; }

    /// <summary>
    /// If <c>true</c> compiler will preserve intermediate compilation artifacts; otherwise they will be deleted.
    /// </summary>
    public bool PreserveBuildArtifacts { get; set; }

    /// <summary>
    /// If <c>true</c> compiler will log debug information; otherwise <c>false</c>;
    /// </summary>
    public bool Debug { get; set; }

    // BUG. Setting this to value > 0 crashes OPA compiler.
    // /// <summary>
    // /// Optimization level.
    // /// </summary>
    // public int OptimizationLevel { get; set; }

    /// <summary>
    /// Exclude dependents of entrypoints.
    /// </summary>
    public bool PruneUnused { get; set; }

    /// <summary>
    /// Set file and directory names to ignore during loading (e.g., '.*' excludes hidden files).
    /// </summary>
    public IReadOnlySet<string> Ignore { get; set; } = new HashSet<string>();
}