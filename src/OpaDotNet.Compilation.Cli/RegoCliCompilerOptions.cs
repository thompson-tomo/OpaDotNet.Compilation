using JetBrains.Annotations;

using OpaDotNet.Compilation.Abstractions;

namespace OpaDotNet.Compilation.Cli;

/// <summary>
/// Contains members that affect compiler behaviour.
/// </summary>
[PublicAPI]
public class RegoCliCompilerOptions : RegoCompilerOptions
{
    /// <summary>
    /// Full path to opa cli tool.
    /// </summary>
    public string? OpaToolPath { get; set; }

    /// <summary>
    /// Extra arguments to pass to <c>opa build</c> cli tool.
    /// </summary>
    public string? ExtraArguments { get; set; }
}