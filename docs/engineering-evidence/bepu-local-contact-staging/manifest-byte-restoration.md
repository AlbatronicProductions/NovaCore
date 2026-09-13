# BEPU manifest byte restoration

2026-09-13. **UNBANKED.** This corrects dependency artifact representation only.
The retained solver draft remains **UNQUALIFIED — NOT PART OF THIS CORRECTION**.

## Baseline and cause

HEAD/main/origin/main remain `f77d97adaf3ea2637214dfdfd6343973f9d147d7`.
Branch: `codex/bepu-local-contact-staging-v2`. No refs, commits, tags or canonical
index entries changed. The previous [build blocker](staging-build-blocker.md)
correctly recorded a banked manifest/pin contradiction.

The banked and initial working manifest contained 5,338 bytes, 88 LF endings,
no CRLF endings, SHA-256:
`777F1634603A4CCCA55E778706498BBD463447F14EA075359B0AEE16F8BC9C03`.

The accepted required hash, restored working hash, and clean-checkout hash are all:
`C76F2DE75DAC81C8695109977E1F8A4DCCAFF29DA0BB96884C46C0B33362240E`.

Exact bytes were recovered from the prior hosted-qualification Git object:
`fbeec05c78359189b25c33109d332f5b8c6474d5:external/bepu/2.5.0-beta.29/manifest/bepu-2.5.0-beta.29.json`.
Its provenance is recorded in [hosted identity](hosted-cache-scope-identity.json).
The recovered blob is `22864af89051d897061b9db2cf9c9c4038205372`, 5,426 bytes,
88 CRLF endings. The sole difference is 88 restored carriage-return bytes.
Parsed JSON values compare equal. No required pin, semantic content, qualification
identity, DLL, archive, license, security policy or dependency architecture changed.

## Checkout preservation

**UNCHANGED — EXISTING RULE SUFFICIENT**.

`external/bepu/**/manifest/*.json -text` already prevents text conversion.
No `.gitattributes` or Git configuration change was made.

A disposable alternate index was loaded from HEAD and overlaid with only the
repaired manifest. `git hash-object --path` and `git checkout-index` exercised Git
storage and checkout conversion. The 201-file dependency-test closure was exported
into a fresh empty directory. Every exported qualified dependency artifact matched
its current source bytes. The new solver files and the earlier owner-thread query
were excluded: this export is the banked source plus the one authorized repair.
The canonical index remained untouched.

## Validation

SDK 10.0.303. Executed from that clean export, in order:

```powershell
dotnet build tests/NovaCore.BepuDependency.Tests -c Debug -p:ContinuousIntegrationBuild=true --nologo -v minimal
dotnet run --project tests/NovaCore.BepuDependency.Tests -c Debug --no-build --no-restore
dotnet build tests/NovaCore.BepuDependency.Tests -c Release -p:ContinuousIntegrationBuild=true --nologo -v minimal
dotnet run --project tests/NovaCore.BepuDependency.Tests -c Release --no-build --no-restore
```

| Gate | Debug | Release |
|---|---|---|
| Relevant build | PASS, 0 warnings/errors | PASS, 0 warnings/errors |
| Manifest/DLL/license integrity | PASS | PASS |
| CI archive SHA-256/SHA-512 verification | PASS | PASS |
| Verified dependency tests | PASS | PASS |

Both executable runs confirmed the exact output/runtime hashes, one loaded identity
per assembly, version 2.5.0.0, expected public-key tokens, framework-only additional
closure, metadata resolution (Physics 736 types; Utilities 91), and deployed license
and attribution. No solver world was created. No solver compilation/qualification
or unrelated regression campaign ran.

### Whitespace check: explicit CRLF interpretation

Unmodified `git diff --check` returns **2**, reporting all 88 required terminal CR
bytes as trailing whitespace. These are exactly the bytes mandated by the approved
artifact hash, not whitespace added to the JSON payload. They must not be removed.

The read-only, command-scoped CRLF-aware check returns **0 / PASS**:

```powershell
git -c core.whitespace=blank-at-eol,blank-at-eof,space-before-tab,cr-at-eol diff --check
```

This retains normal trailing-space, blank-at-EOF and space-before-tab checks while
recognizing CRLF line terminators. It changes neither artifact bytes nor persistent
configuration. The ordinary check's nonzero result is retained explicitly, not
reported as a pass. A later closeout must preserve the qualified bytes.

## Identity and storage

[Machine-readable results](manifest-byte-restoration.json) record all six qualified
artifact hashes, the exact recovered source, checkout identity, build/test results,
and unchanged draft fingerprints. Across 359 checked preexisting source/test/build
and dependency files, only the authorized manifest changed. The earlier source
draft, owner-thread query, permanent tests, attributes and integrity targets remain
unchanged by this correction.

The temporary export/index/preflight data comprise 427 files / 45,394,157 bytes
before cleanup. They are rebuildable and disposable after this report is retained.
No existing investigation directory is part of that cleanup. Final cleanup status
is recorded in the JSON results.

## Reproduction without committing or staging the canonical index

Recover the historical blob with a byte-preserving subprocess and verify SHA-256
before writing. If the old temporary commit is eventually pruned, converting only
the banked LF blob's line endings to CRLF reconstructs the identical hash; hash
equality remains mandatory. The corrected candidate manifest is also a source.

For checkout reproduction, use a new empty disposable directory and an absent
`GIT_INDEX_FILE` path scoped only to child processes:

1. `git read-tree HEAD` into that alternate index.
2. Feed the repaired manifest bytes to `git hash-object -w --path=<manifest> --stdin`.
3. Update only its entry with `git update-index --cacheinfo 100644 <blob> <manifest>`.
4. List and check out `.gitattributes`, `Directory.Build.props`,
   `Directory.Build.targets`, optional tracked `global.json`/`NuGet.Config`,
   `external/bepu`, `src/NovaCore.Core`, `src/NovaCore.EphemerisFormat`,
   `src/NovaCore.Simulation`, and `tests/NovaCore.BepuDependency.Tests` using
   null-delimited paths and `git checkout-index --prefix=<empty-directory>/`.
5. Verify hashes before running the four commands above. Do not copy untracked
   solver drafts into this dependency-only export.

Lead judgment: **PASS** for the exact-byte repair and dependency qualification,
with the default whitespace check's CRLF diagnostic disclosed above.

**BEPU MANIFEST BYTE INTEGRITY — READY FOR PROJECT CONTROL ACCEPTANCE**.
UNBANKED. Stop for Project Control before resuming the separate solver ticket.
