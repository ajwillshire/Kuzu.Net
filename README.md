# Kuzu.Net

A .NET binding for [Kùzu](https://kuzudb.com/) — the embeddable, MIT-licensed property-graph database with openCypher queries.

[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE)

> **Status: early / pre-release (0.1.0).** This repository currently ships the
> project scaffolding and a P/Invoke interop skeleton over the Kùzu C API. The
> native Kùzu binaries are **not yet vendored** and the extern signatures are
> declared against a target Kùzu C API version that **still needs verifying
> against a pinned release** (see [Roadmap](#roadmap)). It compiles; it does not
> yet run end-to-end. Contributions welcome.

## Why this exists

Kùzu publishes official client libraries for C/C++, Python, Node.js, Rust, and
Java — **but not for .NET**. Anyone wanting to use Kùzu from F# or C# today has
to hand-roll P/Invoke over the C API. `Kuzu.Net` aims to be the maintained,
reusable binding that fills that gap: a thin, dependency-light managed layer
over Kùzu's C API that any .NET project can `dotnet add package` and consume.

It is written in **F#** but designed for a **C#-ergonomic public surface** — the
authoring language is invisible to consumers (see [Consuming from C#](#consuming-from-c)).

## Design goals

- **Thin and honest.** A faithful binding over the Kùzu C API, not an
  opinionated ORM or query builder. No high-level abstractions layered on top —
  those belong in a separate package.
- **Safe handle ownership.** Native database / connection / result handles are
  wrapped in `SafeHandle` subclasses so the GC and `IDisposable` reclaim native
  resources deterministically and without leaks.
- **Cross-platform.** Targets `win-x64`, `linux-x64`, and `osx-arm64` via
  RID-specific native payloads (once vendored).
- **C#-friendly boundary.** `Task`-returning async methods, `IReadOnlyList<T>`
  collections, plain classes/enums — no `FSharpOption` or curried members leak
  across the public API. The one deliberate discriminated union is
  `KuzuValueData` (the decoded payload of a result cell): F# consumers get an
  exhaustive `match`, while C# consumers stay on the accessor surface
  (`.Kind`, `.AsString()`, `.AsInt64()`, …) and never need to touch it.

## Install

> Not yet published to NuGet. Once the first runnable release lands:

```sh
dotnet add package Kuzu.Net
```

## Quick start

### F#

```fsharp
open Kuzu.Net

// Open an on-disk database (or KuzuDatabase.OpenInMemory ()).
use db = KuzuDatabase.Open "./demo.kuzu"
use conn = db.Connect()

conn.Query "CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name))"
|> ignore

conn.Query "CREATE (:Person {name: 'Alice', age: 30})" |> ignore

use result = conn.Query "MATCH (p:Person) RETURN p.name, p.age"

for row in result do
    // Accessor surface (shared with C#)...
    printfn "%s is %d" (row[0].AsString()) (row[1].AsInt64())

    // ...or pattern-match the discriminated union for exhaustive handling:
    match row[1].Value with
    | KInt64 age -> printfn "age = %d" age
    | KNull -> printfn "age is unknown"
    | other -> printfn "unexpected: %A" other
```

### Consuming from C#

`Kuzu.Net` is an ordinary .NET assembly — C# consumes it directly. The public
surface is deliberately idiomatic for C#:

```csharp
using Kuzu.Net;

using var db = KuzuDatabase.Open("./demo.kuzu");
using var conn = db.Connect();

await conn.QueryAsync(
    "CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name))");

using var result = await conn.QueryAsync("MATCH (p:Person) RETURN p.name, p.age");
foreach (IReadOnlyList<KuzuValue> row in result)
{
    Console.WriteLine(row[0].AsString());
}
```

> **Note on `FSharp.Core`.** Because `Kuzu.Net` is authored in F#, consuming it
> pulls a transitive reference to `FSharp.Core` into your project. It is a single
> well-maintained package with no further dependencies — but C# consumers should
> be aware it appears in their dependency graph.

## Native dependency

`Kuzu.Net` is a binding, not a reimplementation — it requires the native Kùzu
shared library (`kuzu.dll` / `libkuzu.so` / `libkuzu.dylib`) at runtime. The
intent is to ship RID-specific native binaries inside the package under
`runtimes/<rid>/native/` so consumers get the matching binary automatically on
`dotnet restore`. Until those are vendored, you must place the Kùzu shared
library for your platform alongside your application's output.

Kùzu native libraries are MIT-licensed and published by the Kùzu project.

**The not-yet-vendored state is declared, not merely true.**
[`NATIVE-PROVENANCE.md`](NATIVE-PROVENANCE.md) records what is expected, from
where, under what licence, and how it will be pinned when it lands;
`native-provenance.props` is the machine-readable half, and the build enforces
it. Concretely: a `.dll`, `.so` or `.dylib` appearing anywhere in this
repository fails the build (`NP0004`) naming the file, because nothing here
declares one. Declaring a vendored artefact — upstream release, licence,
SHA-256 — is what makes it green again, and from then on a binary whose bytes
do not match its recorded digest fails the build too. The point is that the
decision about what a native binary is and where it came from happens when it
enters the repository rather than months afterwards.

## Roadmap

- [ ] Verify the Kùzu C API extern signatures against a pinned Kùzu release
      (now includes the `kuzu_data_type_id` numeric mapping and the typed
      value-getter signatures).
- [ ] Vendor RID-specific native payloads (`win-x64`, `linux-x64`, `osx-arm64`).
- [x] Scalar typed value accessors — `KuzuValueData` DU + `.AsInt64`, `.AsDouble`,
      `.AsBoolean` over Bool / Int8–64 / UInt8–32 / Float / Double / String.
- [ ] Structural decode of composite cells (node, rel, list, struct, map,
      temporal) — they currently surface as `KOther <kind>` with Kùzu's string
      rendering available via `.AsString()`.
- [ ] Prepared statements + parameter binding.
- [ ] An end-to-end test suite gated on a downloaded native library.
- [ ] First runnable NuGet release.

## License

Apache-2.0 — see [LICENSE](LICENSE). Kùzu itself is MIT-licensed; this binding
links to it but is a separate work.
