# Reviewed disposable output

2026-09-17. Results, original witnesses, source/log identities and reproducibility
are preserved before this classification. No tracked file or reparse point was
found in these roots. Source backups match restored current files.

| Exact path | Files | Bytes | Disposition |
|---|---:|---:|---|
| `E:\NovaCore\build\srv01-powered-support` | 248 | 47,292,499 | Prior blocked root; no retry; manual cleanup |
| `E:\NovaCore\build\srv01-stage2-closure` | 1,749 | 363,674,552 | Rebuildable qualification outputs/logs; manual cleanup |
| `E:\NovaCore\build\srv01-display-gap-closure` | 376 | 103,897,192 | Rebuildable diagnostic outputs/logs and redundant source backups; manual cleanup |
| `C:\Users\Tyler\AppData\Local\Temp\NovaCore-vulkan-ce7a5b94889c43b9830b7ee4b2897f6a` | 1 | 57,598 | Generated test-only Vulkan layer manifest; manual cleanup |

Removed: **0 files / 0 bytes**. Remaining: **2,374 files / 514,921,841 bytes**.

Automatic approval review rejected the single attempted cleanup of the three new
roots as `blocked by policy`, before execution. No retry or workaround occurred.
The old `srv01-powered-support` root was not included in that attempt.

Exact reviewed manual PowerShell command (all four presently exist):

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\build\srv01-powered-support','E:\NovaCore\build\srv01-stage2-closure','E:\NovaCore\build\srv01-display-gap-closure','C:\Users\Tyler\AppData\Local\Temp\NovaCore-vulkan-ce7a5b94889c43b9830b7ee4b2897f6a' -Recurse -Force -ErrorAction Stop
```

Non-destructive verification:

```powershell
'E:\NovaCore\build\srv01-powered-support','E:\NovaCore\build\srv01-stage2-closure','E:\NovaCore\build\srv01-display-gap-closure','C:\Users\Tyler\AppData\Local\Temp\NovaCore-vulkan-ce7a5b94889c43b9830b7ee4b2897f6a' | ForEach-Object {
    [pscustomobject]@{Path=$_; Exists=Test-Path -LiteralPath $_}
}
```

After manual cleanup all four should report False. Do not retry deletion of a path
that is already absent. Other repository/build/temporary material is outside this
review and is not authorized by this command. Cleanup does not resolve the causal
stop or grant Stage 2 acceptance.
