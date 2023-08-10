# Open Policy Agent (OPA) Compilation Tools

Infrastructure for packaging OPA policy and data files into bundles for [OpaDotNet](https://github.com/me-viper/OpaDotNet) project.

## NuGet Packages

|                                       | Package  |
|---------------------------------------|----------|
| OpaDotNet.Compilation.Abstractions    | [![NuGet](https://img.shields.io/nuget/v/OpaDotNet.Compilation.Abstractions.svg)](https://www.nuget.org/packages/OpaDotNet.OpaDotNet.Compilation.Abstractions/) |
| OpaDotNet.Compilation.Cli             | [![NuGet](https://img.shields.io/nuget/v/OpaDotNet.Compilation.Cli.svg)](https://www.nuget.org/packages/OpaDotNet.Compilation.Cli/) |
| OpaDotNet.Compilation.Interop         | [![NuGet](https://img.shields.io/nuget/v/OpaDotNet.Compilation.Interop.svg)](https://www.nuget.org/packages/OpaDotNet.Compilation.Interop/) |

## Getting Started

Which one you should be using?

Use [`OpaDotNet.Compilation.Cli`](./src/OpaDotNet.Compilation.Cli) if you have `opa` CLI [tool](https://www.openpolicyagent.org/docs/latest/cli) installed or you need functionality besides compilation (running tests, syntax checking etc.). Suitable for web applications and/or applications running in Docker containers.

Use [`OpaDotNet.Compilation.Interop`](./src/OpaDotNet.Compilation.Interop/) if you need compilation only and want to avoid having external dependencies. Suitable for libraries, console application etc.

### Cli

#### Install OpaDotNet.Compilation.Cli nuget package

```sh
dotnet add package OpaDotNet.Compilation.Cli
```

#### Usage

> [!IMPORTANT]
> You will need `opa` cli tool v0.20.0+ to be in your PATH or provide full path in `RegoCliCompilerOptions`.

```csharp
using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Cli;

IRegoCompiler compiler = new RegoCliCompiler();
var bundleStream = await compiler.CompileFile("example.rego", new[] { "example/hello" });

// Use compiled policy bundle.
...
```

### Interop

#### Install OpaDotNet.Compilation.Interop nuget package

```sh
dotnet add package OpaDotNet.Compilation.Interop
```

#### Usage

```csharp
using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Interop;

IRegoCompiler compiler = new RegoInteropCompiler();
var bundleStream = await compiler.CompileFile("example.rego", new[] { "example/hello" });

// Use compiled policy bundle.
...
```
