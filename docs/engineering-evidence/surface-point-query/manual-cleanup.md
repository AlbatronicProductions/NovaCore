# Manual cleanup of proven external generated output

**PROJECT CONTROL CLEANUP REQUIRED.** The bounded performance follow-up preserves
the original 683 files / 45,650,081 bytes and adds classified private benchmark
output below. See [current group counts](qualification/manual-disposable-inventory.json)
and the [current qualification report](performance-qualification.md) for final
parity output and total accounting. These are logical byte counts, not NTFS allocation.

Automatic tool policy rejected the broad recursive command before shell execution; no detailed reason beyond `blocked by policy` was supplied. No broad retry or alternate workaround was attempted. Narrow raw-file cleanup succeeded. These paths are external generated output from this proof, with no production consumer. Preserve README, private-capture.patch, prepare.py, parity.py, candidate.py, compare.py and JSON/log evidence.

The following PowerShell commands are for Project Control to run manually if desired, after reviewing `disposable-remaining.json`. They delete only the exact proven generated directories/files; they do not touch E:/NovaCore.

```powershell
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-parity-20260909\baseline-source' -Recurse -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-parity-20260909\baseline-libs' -Recurse -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-parity-20260909\host-baseline' -Recurse -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-parity-20260909\host-candidate' -Recurse -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-parity-20260909\native-build' -Recurse -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-parity-20260909\banked-deployment' -Recurse -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-parity-20260909\private' -Recurse -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-parity-20260909\baseline-source.zip' -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-parity-20260909\refine-runner.py' -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-parity-20260909\make-reproducible.py' -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-parity-20260909\consolidate.py' -Force
```

The separate CPU-only query proof left 32 rebuildable files / 1,375,750 logical
bytes under these two directories. The two source files in their parent must be
retained; neither directory is used by normal NovaCore deployment.

```powershell
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-point-validation-20260909\cpu-only\bin' -Recurse -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-point-validation-20260909\cpu-only\obj' -Recurse -Force
```

## Bounded performance qualification output

The following nine exact groups add **80 files / 5,319,400 bytes**: cost prototype
build output and two superseded early summaries (34 files / 1,455,607 bytes), plus
the actual-DLL benchmark hosts/intermediates (46 files / 3,863,793 bytes). No
production runtime, asset generator or permanent test consumes them. Their parent
source recipes, final summaries and identity snapshots remain outside these groups.

Automatic review rejected the cost worker's command containing removal of the two
early summaries, stating only `blocked by policy`. No alternate deletion or retry
occurred. This does not prevent engineering acceptance; cleanup remains a separate
Project Control action before banking unless explicitly accepted for retention.

```powershell
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-qualification-20260909\cost-proof\bin' -Recurse -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-qualification-20260909\cost-proof\obj' -Recurse -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-qualification-20260909\cost-proof\forward.json' -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-qualification-20260909\cost-proof\reverse.json' -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-qualification-20260909\verify-b\actual-benchmark\bin' -Recurse -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-qualification-20260909\verify-b\actual-benchmark\obj' -Recurse -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-qualification-20260909\verify-b\actual-baseline' -Recurse -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-qualification-20260909\verify-b\actual-candidate' -Recurse -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-qualification-20260909\verify-b\starting-assemblies' -Recurse -Force
```

The preserved `verify-b/starting-source` and `starting-tracked.diff`, banked HEAD,
assembly/source fingerprints, benchmark source, runner and comparison scripts
retain the reconstruction boundary after these copied DLLs are removed. Do not
delete the whole research parent directory or ordinary NovaCore build/deployment.

## Final corrected-candidate raster parity

Three further groups are disposable after exact parity passed: the private final
host (67 files / 6,830,363 bytes) and six raw files (253,782,448 bytes). Their
digests, exact execution/runtime identity, result comparison and reproduction
script are retained. No deletion was attempted during the two final runs.
The earlier removed 507,564,896 bytes remain removed; these are new bounded
verification captures, not rediscovered old debris.

```powershell
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-parity-20260909\host-final-qualified' -Recurse -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-parity-20260909\raw-final-qualified-florida\pixels.bin','E:\NovaCore-Research\surface-query-parity-20260909\raw-final-qualified-florida\prepared.bin','E:\NovaCore-Research\surface-query-parity-20260909\raw-final-qualified-florida\selected.bin' -Force
Remove-Item -LiteralPath 'E:\NovaCore-Research\surface-query-parity-20260909\raw-final-qualified-inland\pixels.bin','E:\NovaCore-Research\surface-query-parity-20260909\raw-final-qualified-inland\prepared.bin','E:\NovaCore-Research\surface-query-parity-20260909\raw-final-qualified-inland\selected.bin' -Force
```

**Final classified disposable remaining: 836 files / 311,582,292 logical bytes
across 25 exact groups.** All groups are outside Git and outside production
deployment. Inventory was remeasured after validation. Keep compact reports,
hashes, source recipes, execution metadata and permanent repository tests.
