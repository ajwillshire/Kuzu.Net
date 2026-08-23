# Native-artefact provenance

**Nothing native is vendored in this repository today, and this document is the
record of that rather than the absence of one.**

`Kuzu.Net` is a P/Invoke binding: `src/Kuzu.Net/NativeMethods.fs` declares its
externs against the Kùzu C API under the library name `kuzu`, and the .NET host
resolves that name at run time from whatever the consuming application supplies.
No native payload is vendored for any runtime identifier — see the status note
at the top of [`README.md`](README.md) and the "Vendor RID-specific native
payloads" item in its roadmap.

A companion with no provenance file and a companion with nothing to have
provenance *for* look identical from outside. This file, and the
[`native-provenance.props`](native-provenance.props) declaration beside it, are
what tell them apart.

## What is expected, when it lands

| Field | Value |
| --- | --- |
| Upstream | https://github.com/kuzudb/kuzu |
| License | MIT |
| Version pin | **none yet** — the extern signatures target the Kùzu C API and still need verifying against a pinned release |
| Intended RIDs | `win-x64`, `linux-x64`, `osx-arm64` |
| Expected artefacts | the Kùzu C API shared library (`kuzu_shared.dll` / `libkuzu.so` / `libkuzu.dylib`) under `runtimes/<rid>/native/` |
| Pin when vendored | **SHA-256 of the downloaded release asset** — upstream publishes prebuilt archives, so what gets vendored is a binary held to its digest, not a local build |

Note the last row is a real decision rather than a placeholder. Where a
companion builds its native dependency from source, the honest pin is the
upstream tag plus the build script, because two toolchains produce different
bytes. Kùzu ships prebuilt releases, so a vendored payload here *can* be pinned
by digest, and should be: a downloaded artefact is exactly the case a digest is
for.

## The declaration is enforced

The build fails (`NP0004`) when a native binary — `.dll`, `.so`, `.dylib` —
appears anywhere in this repository that the manifest does not cover. Since the
manifest currently covers nothing, that means **any** native library in the tree
turns the build red and names it.

That is deliberate, and it is the whole value of the declaration while nothing
is vendored. Making the build green again means recording what the binary is,
where it came from, under what licence, and its SHA-256 — the conversation that
should happen at the moment a native binary enters a repository, rather than
months later when nobody can reconstruct which build it was.

## Vendoring procedure, when the time comes

1. Download the upstream release asset for each RID; record the release version
   and the SHA-256 of each artefact as downloaded.
2. Place each under `runtimes/<rid>/native/`, and decide deliberately whether the
   binaries are committed or fetched by a script. **A committed binary is
   hash-pinned; a fetched one needs the fetch script to verify the digest before
   it is used** — a companion that downloads a native library without checking
   what arrived has no provenance regardless of what this file says.
   `.gitignore` currently excludes `runtimes/*/native/*`, so that decision has a
   default today that nobody has actually taken.
3. Declare each artefact in [`native-provenance.props`](native-provenance.props)
   as a hash-pinned `NativeArtefact` — upstream repository, release version,
   licence, SHA-256, size — replacing the `NativeNotVendored` declaration.
4. Replace the table above with the pinned facts, and keep this document's
   digests in step with the manifest: the build checks that too (`NP0003`).
5. Verify the extern signatures against the pinned release before the binding is
   run — a mismatched struct layout across a foreign-function boundary is a
   silent memory-corruption hazard, not a compile error, and it is the open
   roadmap item this repository is most exposed to.

## What a pin does not claim

A digest detects substitution *after* the moment of recording. It says nothing
about the supply chain upstream of that moment: an upstream release that was
itself compromised and then pinned faithfully is pinned faithfully. And nothing
about a pin bounds what the native library does once loaded — it runs in-process
with full authority, and no digest changes that.
