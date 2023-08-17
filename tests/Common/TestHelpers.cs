namespace OpaDotNet.Compilation.Tests.Common;

internal static class TestHelpers
{
    public const string SimplePolicySource = """
        package example
        import future.keywords.if
        default allow := false
        """;

    public static readonly string[] SimplePolicyEntrypoints = { "example/allow" };
}