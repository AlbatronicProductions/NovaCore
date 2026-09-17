# Reviewed SRV-01 disposable output

2026-09-17. Exact owned root: `E:\NovaCore\build\srv01-integration`.
Reviewed before cleanup: **1,925 files, 382,623,053 bytes**, no reparse points.
No application process remained open. See disposable-inventory.json for final
disposition and the individual root-level file list.

Automatic approval review rejected the reviewed deletion before execution as
`blocked by policy`. No automatic retry or alternative deletion mechanism was
used. The exact command below was supplied to Project Control. Until independent
verification records otherwise, all 1,925 files / 382,623,053 bytes remain pending
manual cleanup; this is not a new engineering failure.

| Exact directory / bucket | Files | Bytes |
|---|---:|---:|
| E:\NovaCore\build\srv01-integration\artifacts | 1,467 | 323,898,600 |
| E:\NovaCore\build\srv01-integration\exhaust-correction | 118 | 2,371,035 |
| E:\NovaCore\build\srv01-integration\exhaust-ksa | 2 | 15,257 |
| E:\NovaCore\build\srv01-integration\ksa | 7 | 146,261 |
| E:\NovaCore\build\srv01-integration\launcher | 24 | 3,009,994 |
| E:\NovaCore\build\srv01-integration\native-debug | 120 | 43,081,450 |
| E:\NovaCore\build\srv01-integration\native-release | 99 | 6,249,940 |
| E:\NovaCore\build\srv01-integration\proof | 4 | 142,967 |
| E:\NovaCore\build\srv01-integration\visual-intake | 32 | 1,403,147 |
| E:\NovaCore\build\srv01-integration (root-level files only) | 52 | 2,304,402 |

All are **DISPOSE**: rebuildable bin/obj/native output, raw diagnostics, read-only
KSA decoding scratch, duplicate accepted-asset intake and reproduced numerical
proof. No source candidate, accepted GLB, third-party install, reference video,
Blender source, or unrelated legal-review material is included.

Concise results, failure history, source/runtime/asset hashes, direct provenance,
manual acceptance and reproduction instructions remain in this evidence directory.
The current permanent tests/source are the reproducible contract. The retained
oracle adaptation replaces its former scratch dependency. Do not run intake again
or copy KSA assets to rebuild NovaCore.

Exact reviewed manual cleanup command (retained even if automated cleanup succeeds):

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\build\srv01-integration' -Recurse -Force -ErrorAction Stop
```

Non-destructive verification, expected `False` after cleanup:

```powershell
Test-Path -LiteralPath 'E:\NovaCore\build\srv01-integration'
```

Do not rerun deletion against an absent path. A later reproduction intentionally
recreates this root and needs a fresh inventory before another cleanup.
