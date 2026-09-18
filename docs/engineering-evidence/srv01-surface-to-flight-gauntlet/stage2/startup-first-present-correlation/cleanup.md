# Targeted diagnostic cleanup

Exact disposable output: `E:\NovaCore\build\srv01-startup-first-present` — **386 files / 103968588 bytes** before cleanup. It contains only the isolated build, temporary source backups and raw logs. Original source bytes were restored before captures; exact useful timestamps, accounting, identities, summaries and deterministic reproduction inputs are retained here.

Cleanup is consolidated after Stage-2 evidence/manual completion in [final-acceptance/cleanup.md](../final-acceptance/cleanup.md) and its machine-readable `cleanup.json`. The previous population root is inventoried there and is not automatically retried.

Reviewed exact manual PowerShell command, if still present:

```powershell
if (Test-Path -LiteralPath 'E:\NovaCore\build\srv01-startup-first-present') {
    Remove-Item -LiteralPath 'E:\NovaCore\build\srv01-startup-first-present' -Recurse -Force -ErrorAction Stop
}
```

Non-destructive verification:

```powershell
Test-Path -LiteralPath 'E:\NovaCore\build\srv01-startup-first-present'
```

Expected after cleanup: `False`. Actual disposition is retained in the consolidated record; no blocked deletion is retried.
