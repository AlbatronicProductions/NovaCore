# Cached request-key parity tooling review

These are diagnostic helpers for the current M13.5 exit ticket. The review agent wrote `capture.py`, `parity.py` and `candidate_parity.py`; the lead owns `build_capture.py`, all builds and all runtime execution. No production source, old scratch directory or earlier evidence package was changed by this subtask.

## Capture and comparison boundary

The private native capture copies one D32 depth attachment, one RGBA16F HDR attachment and the final four-byte-per-pixel image at frame 175. After the submission fence completes, it reads the current prepared physical vertex records and selected indices. It snapshots generation/pupil at recording and rejects a changed frame/publication before readback. Each record includes all 64 physical bytes. The triangle comparison sorts triangle records as units: it retains winding and duplicate multiplicity while allowing parallel compaction order to differ.

The input gate compares current/incoming generation and topology, the complete camera, planetary GPU inputs, presentation and lighting, published/incoming pupil bytes, preparation cursor/activity/readiness, workload counters, tessellation factors, command line and environment. The retained correlation hook zeroes only the logged `terrainFrame` field; it does not change the submitted field. Publication/pupil identity is recorded separately. A fixed-frame capture does not prove transition scheduling or lifetime negative cases.

The two processes use the same private binary, managed driver and shaders. Only `NOVACORE_EXIT_CACHED_KEYS` may differ. No earlier working-data placement override, shader substitution, clipping control or counter-disable flag is imported. Both processes have identical capture instrumentation and fixed-pose/time controls. Capture-enabled timings are excluded from performance evidence.

The single raw slot is `build/m13-final-exit/parity-slot`. It permits exactly `pixels.bin`, `prepared.bin` and `selected.bin`; rejects reparse/linked raw files and unclassified entries; and enforces a 512 MiB bound. It overwrites this slot between runs, never an earlier evidence directory. No deletion command is provided by these helpers. All named journals and final reports reject existing `.json` and `.json.gz` files.

`candidate_parity.py` selects `private-candidate-capture-host.json` in process, with the same source-restoration and deployment-fingerprint guards. It runs the implemented default with the cached-key diagnostic flag absent. It allows the expected native binary change, but requires the same managed driver, shaders, inputs and execution environment. It compares complete attachment digests against the successful current-ticket A/B report, without retaining or reloading earlier raw attachments. An input or output mismatch fails; an A/A comparison cannot waive failure.

## Verification performed by the review agent

- Python AST parsing passed for all three helpers.
- Pure capture transformers matched every required anchor in current native source and `Program.cs`; transformed text was held in memory only.
- Inspection confirmed the readback hook follows `vkWaitForFences` and precedes the next publication inspection/update.
- In-memory comparison checks passed for exact equality, expected native hash change, and triangle order-only variation. They rejected an attachment byte change, changed composition, changed workload, changed physical digest, changed oriented triangle multiset and changed managed driver.
- Current module paths resolved to this ticket's helpers. The candidate manifest selector compiled without launching a process.
- All three files are UTF-8 readable and have no trailing whitespace. The system Python lacks NumPy; execution uses the available bundled Python environment, as the lead's actual captures do.

The review agent performed no build or GPU run. Independently reading the lead's completed `keys-florida-parity.json` and `keys-inland-parity.json` established both A/B reports passed: full physical bytes and oriented triangle multisets matched, and D32/HDR/final image each had zero changed pixels. Selected buffer order differed in both, as expected for parallel compaction; the stronger oriented multiplicity check passed. Florida used 128,369,384 raw bytes, inland 124,943,408, each within the single-slot limit.

This does not claim implemented-default parity has executed; that requires the separately fingerprinted candidate capture and its own report. The cached-key optimization concerns CPU readback inspection. These correctness results do not establish a GPU timing improvement.

## Retained hook provenance

Only pure source text/functions were adapted or read from the following retained files. No earlier runner was invoked and no prior scratch path was used.

| Retained source | SHA-256 |
| --- | --- |
| `post-m13.3-next-target/capture.py` | `a4c7c7afaaf9fa89201854fe846bd899674478015cf2457381a2ea5b6c9aac26` |
| `post-m13.2-next-target/instrumentation.patch` | `2c5014a949df9ee9d5e82a8c994bc9ed3ce6e856ed2e211fe2f97263adeb35f2` |
| `m13-regional-preparation-convergence/correlation.inl` | `b70a0ce9fdd5990d93e90fa4cdcc2708a197a9e8ca7e46a4d73811fb985cb775` |
| `m13-final-whole-frame-causality/placement_parity.py` | `58bd1cfec7f0a39b5702abfb70c285ef807eea35b1471e3c41546370166193bf` |
| `m13-final-whole-frame-causality/candidate_parity.py` | `6da393b98c2f8efa09db13680c65bbaad7319b461e3c6e95be13b1b580bd490d` |

Current A/B report SHA-256 values before any lossless compression:

- Florida: `49d1af1f2927a55ee198bad6de7347ef6c6ba2e65d893612bcead31e4f3c415a`.
- Inland: `3f187efe933fcb70ab9a601273b27fb3184fe34e03ee322003a402493ddf4a7d`.

The durable package should retain helper source, compact reports, provenance and losslessly compressed journals within the ticket's 6 MiB budget. Raw attachments remain disposable after accepted comparison; they are not permanent fixtures.
