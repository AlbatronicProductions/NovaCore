# Reproduce the bounded material proof

This package describes an unbanked candidate. Do not treat retained measurements
as fresh measurements on another GPU, driver, compiler or source revision.

Use a separate development checkout of M13.3
`180eaf150ba5db6364e17dd48336690778f058f9`, with this evidence directory and the
candidate's single material-source change copied into the same relative paths.
The checkout needs the normal verified production assets/cache, generated NCSM1
topology, Visual Studio C++ tools, .NET 10 and Vulkan SDK 1.4.357.0. Follow the
repository build/asset instructions; do not copy or duplicate hundreds of
megabytes of assets into this evidence package. A checkout on the same volume can
use existing authorized asset provision. Never run two GPU measurements together.

The original commands ran on RX 6800 XT, native 3440 x 1440. Runtime arguments,
environment, binary/shader/asset hashes and all 100 warm samples are in the JSON
records. Use `records.read(path)` to expand losslessly stored numerical columns.
Times use nearest-rank percentiles; the final window is frames 113 through 212.

## Preparation and fresh labels

Build/deploy normal Debug and Release first using the repository workflow.
`python -B prepare.py` (from this directory) verifies pinned input sources,
current build/deployment agreement and 49 shader identities, builds one private
measurement host, then restores temporarily patched source and the normal build
DLL. It accepts either the verified M13.3 fragment or this candidate. It does not
silently accept other source changes or deployment hashes. It uses the retained
managed fixed-pose patch in `../post-m13.2-next-target/instrumentation.patch` and
the local `instrument.py` / `capture.py` recipes. No old raw archive is required.

Use fresh result labels such as `repro-active-a` and `repro-active-b`; existing
measurement labels deliberately refuse overwriting. Run in the separate checkout
because preparation metadata and validation summaries are written there. Preserve
this original archive. The build currently uses the installed Visual Studio 18
Community developer shell; adjust its explicit path only if the compiler is
installed elsewhere and record that difference.

`run.py` recreates the banked fragment from pinned M13.3 Git source when scratch
is absent. Its SPIR-V must match the original hash, so compiler drift fails
explicitly. Other inputs are protected by preparation's pinned-source check.
`--zero` compiles the precise proposed material change from that pinned baseline;
it never relies on a future mutable HEAD.

## Minimum proof

From this directory, in the separate prepared checkout:

```powershell
python -B run.py active repro-active-a --quiet
python -B run.py active repro-active-b --quiet --zero
python -B run.py florida repro-florida-a --quiet
python -B run.py florida repro-florida-b --quiet --zero
```

Repeat paired commands for `orbital`, `factor1` and `grazing` only for a full
five-pose validation. Without `--quiet`, the private host also records CPU,
preparation and clipping workload. Those records are separate from final timing.
`--isa` retains bounded AMD ISA and resource summaries; never use ISA extraction
runs as final performance numbers. The `--cheap` and `--one` flags are explicitly
output-changing causal sensitivities, not candidate quality settings.

The normal native-DLL confound control is reproducible with:

```powershell
python -B run.py active repro-native-a --quiet --normal-native
python -B run.py active repro-native-b --quiet --normal-native --zero
```

This replaces only the private host's native DLL for each child process and
restores it in a finally block. The reported hash is the DLL used by that child.
The fixed-pose managed diagnostic host remains necessary; normal production DLL,
launcher and shaders are not overwritten by these commands.

## Exact output and normal regression

`parity.py` requires Python with NumPy (the original used the bundled Codex Python
runtime). `python -B parity.py active repro-parity` checks one frame 175 per side:
entire D32, RGBA16F HDR and final BGRA8 image, prepared bytes, and the oriented
submitted triangle multiset. `--mixed` creates alternating zero, partial and full
land-mask lanes to challenge derivatives/helper participation. `--aa` compares
banked against banked. Inland control is `land`; other validated controls are
`florida` and `grazing`. Capture values are finite; no tolerance is introduced.
Parallel compaction does not promise raw index-buffer order. Original failed
ordering and one-pixel Florida records are preserved and explained in README.

At most one scratch set of pixels, prepared vertices and selected indices exists;
it is overwritten between runs. Do not add a full sequence archive. Preserve
paired hash/difference summaries and delete raw files after resolution.

`validate.py tests` runs the normal deployed Debug/Release Graphics categories,
native GPU tests, native regional CPU test with its real production pack, launcher
regressions and asset verification. `resume-tests` is the recorded campaign
continuation after the first runner omitted that pack argument; it skips already
completed Debug categories. The Vulkan tests use the repository's process-local
Khronos-only profile, with validation errors fatal. `routes` reconstructs the
retained six-route probe from `../graphics-validation/package-2.json` and performs
a 500-frame normal Florida smoke. `dynamic` runs the normal full traversal and
the existing warp-only traversal; neither records raw attachments.

`anchor_probe.py` is a private copy of the unchanged headless test. It prints the
four compound-assertion predicates after allocation measurement and before the
unchanged assertion. Its twenty fresh-process checks diagnose the observed
intermittent failure without changing production source, fixture or expectations.
This probe must not substitute for the canonical test.

Finally compare source/deployment hashes, run `git diff --check`, consolidate
small records, and remove only the manifest-classified new scratch output.
Preserve the normal deployed runtime and production assets. Do not bank results
without Project Control acceptance.
