# One production responsibility: terrain working-buffer placement

The cheap continuation gate passed before production edits. Repeated fixed
Florida and exact-input regional controls demonstrated meaningful whole-frame
benefit. Florida and inland first A/B captures passed exact prepared-value,
oriented-triangle, D32, HDR and final-image comparisons. The production change
is limited to the storage policy of the existing persistent terrain buffers.

## What changes

| Contract | Before | Candidate |
|---|---|---|
| Resource owner | Native terrain topology and current/incoming/spare work sets | Same owner |
| Creation frequency | Capacity creation/growth, immutable topology acquisition | Same frequency |
| Required memory | Compatible host-visible, host-coherent type | Same requirements |
| Placement choice | First compatible required type | Prefer compatible local GPU memory for explicitly identified terrain working data |
| Fallback | Original coherent host allocation | Original compatible allocation if preferred type is absent, or preferred allocation/mapping fails for a capacity reason |
| Publication | Existing complete-data/fence/atomic owner transition | Unchanged |
| CPU access | Persistent mapped pointers | Same production contract; opt-in live probe snapshots three completed ranges before repeated CPU analysis |
| Shaders and precision | Banked M13.4 shaders and canonical prepared physical values | Unchanged |

The six call-site roles are prepared physical storage, regional physical staging,
immutable topology lattice, immutable topology indices, visibility output and
compacted index output. These form one GPU working-data responsibility, rather
than a mixture of shader optimizations. Counters, indirect/control blocks,
material upload staging and unrelated scene buffers retain their existing path.

Five populated fixed L17 allocations total 96,846,304 bytes. A regional staging
allocation is additionally selected when that existing responsibility grows.
No new production resource, cache, frame slot or capacity is introduced. The existing
bounded resource counts and teardown govern retention; placement can move
existing capacity into a different heap. This is not a reduction in total bytes.

The private physical-only control is narrower and showed less consistent payoff.
The broader working-data control also reduced compacted-index traffic. The
production API therefore uses explicit `MappedBufferUse::TerrainGpuWorkingSet`
at the six calls; it does not classify ownership from diagnostic failure strings.

## Allocation and failure boundary

`MappedBufferMemory.h` filters the buffer's compatible memory bits and requires
both host visibility and coherence. Locality never becomes a new hardware
requirement. The allocator attempts the preferred type once and may use the
original fallback after out-of-device-memory, out-of-host-memory or mapping
capacity failure. It does not retry a device-loss/other error, or the same type
twice on UMA. Failure of the final attempt remains fatal through strict native
error reporting.

Tentative memory is mapped before binding. A failed map frees its allocation
before a possible retry. A bind failure unmaps/frees the tentative memory and
remains fatal. App memory/mapped handles are assigned only after successful
binding. Allocation sizes, buffer usage, zero initialization, descriptors,
barriers, current-owner retention and destruction are unchanged.

The preferred path improves GPU access on the measured RX 6800 XT. Neither
performance equivalence across other hardware nor successful local allocation
under every memory-pressure condition is assumed. The compatible fallback
preserves the original usable memory contract, without promising the same
performance gain there.

## Payoff and fidelity

The actual default implementation's complete matched 64-frame regional window
recovers 2.423740 ms mean GPU time. Its fixed Florida median recovers
1.87820–2.19040 ms against the bracketing baseline runs. No placement environment
flag is needed by the implemented candidate. This meets the new meaningful
payoff bar, but does not meet 8.33 ms everywhere: all 64 transition samples
remain over 8.33 ms and 61 remain over 11.11 ms.

KSA provides the compatible precedent of persistent GPU-local generated mesh
and visibility resources. NovaCore intentionally retains its mapped/coherent
host access and canonical complete physical-publication contracts. It does not
copy KSA code, replace physical data with a coarse fallback, or adopt KSA's
per-frame physical generation cadence.

The positive-noise sensitivity control changes the material field and is not a
production correction. TES, refinement capacity, physical support, facility
lighting, regional residency and positive-contribution material detail are
preserved. All production shader hashes remain banked M13.4 hashes.

## Tests and retirement

Full validation exposed one diagnostic consequence: the unchanged Debug live
probe timed out after 600 seconds while repeatedly scanning GPU-local mappings.
The sole bounded revision copies completed physical, selected-index and source-index
ranges once into temporary CPU vectors inside the existing opt-in after-fence
probe. Its equations, sample set, outputs, test assertions and timeout remain
unchanged. An empty selected range is not copied. Debug's complete window category
then passed in 158.034 seconds; Release also passed. This transient diagnostic
snapshot is not a normal-frame CPU mirror, new GPU allocation or performance
credit. Two other opt-in one-shot lineage/projection traces retain direct mapped
scans; no recurring ordinary production bulk scan was found.

KEEP all existing physical, graphics, window, launcher and route tests. No test,
fixture, shader or asset is retired or weakened. ADD the permanent native mapped
memory policy test: compatible bits, required properties, unchanged ordinary
selection, preferred success, capacity/map fallback, final failure, device loss,
missing memory and UMA single-attempt behavior. These fourteen cases run in
Debug and Release; resource cleanup is also independently reviewed in the actual
native implementation.

Reproduce this CPU contract after configuring the existing native build:

```powershell
cmake --build build/native-ninja-release --target NovaCoreMappedBufferMemoryTests
& .\build\native-ninja-release\NovaCoreMappedBufferMemoryTests.exe
```

Use `native-ninja` for Debug. Full candidate validation results are recorded
separately in `validation.json`, with exact-output comparisons in the placement
and candidate parity reports. Diagnostic capture timings are excluded from the
performance conclusion.

The runtime needs none of the private flags, source patches, capture buffers or
historical evidence. Retire private binaries/raw output after verification;
retain bounded provenance and reproduction recipes. Project Control owns
acceptance/banking. Proposed title, only for its review:

**NovaCore M13.5: Prefer local GPU memory for terrain working data**
