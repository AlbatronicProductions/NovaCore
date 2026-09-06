# Diagnostic output guidance

The authoritative repository policy is
[ENGINEERING_RULES.md — Diagnostic evidence lifecycle](../ENGINEERING_RULES.md#diagnostic-evidence-lifecycle).
Read it for retention decisions, acceptance gates, video/fixture/tooling treatment,
capture-heavy storage reporting and the Git/LFS boundary. This page provides
archive recovery guidance; it does not define a second policy.

Bounded historical packages may live under `docs/engineering-evidence/<milestone>/`.
Each package records its size budget, retention reasons, retained-file hashes,
retired-output inventory, acceptance authority and any missing original evidence.
An index maps historical paths to reports, provenance, selected visuals and
diagnostic source/recipes. Retention in the workspace is separate from explicit
promotion of large artifacts for Git/LFS inclusion.

Archived source is historical evidence, not another production implementation.
Do not run archived mutation scripts against the current checkout. Restore only
needed diagnostic tooling into a new isolated `build/` directory and deliberately
adapt recorded paths/toolchains. Reproduction need not produce byte-identical old
executables, timings or every historical frame. Analysis requiring retired raw
data must recapture it; absent data must never silently count as a passing test.

For the completed Earth/Solar/Florida investigations, original `build/` paths in
historical reports identify evidence intentionally retired during consolidation.
The [evidence index](engineering-evidence/earth-route-convergence/README.md) and its
manifests preserve the durable report/provenance/reproduction mapping. Original
measurements and acceptance chronology remain unchanged. See the
[cleanup report](diagnostic-evidence-consolidation.md) for the measured result.

The existing accepted Florida recording and bounded package remain untouched by
this policy normalization. Their retention is documented in that index; it does
not establish a default to retain or commit future acceptance recordings.
