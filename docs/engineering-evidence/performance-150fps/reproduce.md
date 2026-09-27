# Reproduction and authority inventory

Canonical root E:\NovaCore; intended unbanked source is fingerprinted by
build/performance-150fps/stage-a/source-freeze.json and the compact results.json.
HEAD alone is not the candidate identity. Preserve the entry files and unrelated
unbanked work; do not reset to HEAD to reproduce this candidate.
The final [bank-candidate report](bank-candidate.md) and [receipt](bank-candidate.json)
supersede earlier candidate identities. Refresh package/preservation receipts with
the report's read-only commands, run `python docs/engineering-evidence/performance-150fps/seal-bank-candidate.py`,
then run the compact evidence sealer last. The full working-file inventory excludes
only the two generated receipt/manifest self-references documented in that receipt.

CPU-only qualification (PowerShell 7):

```powershell
pwsh -NoProfile -File tools/physics/qualify-performance-150.ps1
pwsh -NoProfile -File tools/physics/qualify-recorder-maintenance.ps1 -Configurations Debug
pwsh -NoProfile -File tools/physics/qualify-recorder-maintenance.ps1 -Configurations Release
```

The full driver covers solution builds, reference frames, simulation, surface
contact, swept clearance, exact terrain reuse, modular construction/flight,
scalable launch support and recorder fault/durability/retention suites. Final
maintenance-driver runs supersede earlier intermediate retention results.
The UI probe uses --qualify-storage-ui, creates the real maintenance form HWND
then reenters normal WinForms initialization; it never starts recorder/GPU or
writes runtime evidence. Deletion/capsule adversaries use disposable fixture roots.

Native exposure is already consumed; these are analysis commands, not relaunch:

```powershell
python tools/physics/analyze-post-contact.py --root build/performance-150fps/stage-a --output build/performance-150fps/stage-a/analysis.json
python tools/physics/summarize-post-contact-journal.py "$env:LOCALAPPDATA\NovaCore\MinimumRecorder\ea6841ccd91540c4b0ce3b0fc60b2b28" build/performance-150fps/stage-a/journal-summary.json --flight-report build/performance-150fps/stage-a/native.json
python tools/physics/seal-performance-150.py --output build/performance-150fps/stage-a/recheck-seal.json --compare build/performance-150fps/stage-a/source-freeze.json
python tools/physics/analyze-final-gpu-payoff.py
python tools/physics/seal-performance-150-evidence.py
```

Do not rerun native witness-performance-150.ps1 without a newly qualified and
authorized correction. The consumed marker is not disposable retry state.
The canonical player package remains tools/NovaCore.App/bin/Release/net10.0-windows;
source/package identities and protected inputs were equal before/after exposure.

KEEP: all previous forensic sessions and protected evidence; new pinned session
ea6841ccd91540c4b0ce3b0fc60b2b28; canonical package and input flight save; compact
results/proofs/reports and permanent tests. Retained raw native/timing/log files
are forensic measurements, not replaceable by merely rerunning a build. The
offline build/test outputs are regeneratable, but no build/evidence cleanup is
authorized or performed here. No source, asset, tag or Git-history retirement.

The seal script hashes entry preservation, selected raw evidence, final tests and
reports. It omits bulk arrays from compact results while retaining original file
hashes and paths. No KSA proprietary code is retained in this package.
