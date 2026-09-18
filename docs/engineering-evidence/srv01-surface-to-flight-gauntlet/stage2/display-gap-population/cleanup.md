# Reviewed disposable output

Earlier four reviewed roots: verified absent before this campaign. No deletion of those paths was retried.

This campaign created one bounded disposable root:

`E:\NovaCore\build\srv01-display-gap-population`

**400 files; 106,016,279 bytes.** Exact immediate-child path/count/byte inventory is in cleanup.json. Includes native/managed build trees, bin/obj, copied runtime assets/dependencies, raw capture/build stdout, nine process records and two temporary source backups. No unique production source or authored asset lives here. Reproduction inputs and concise results are retained under this evidence directory; source and output identities are in preflight.json/identity.json. Independent raw-log verification completed before disposal.

Reviewed exact manual PowerShell cleanup command (only if the path still exists):

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\build\srv01-display-gap-population' -Recurse -Force -ErrorAction Stop
```

Non-destructive verification:

```powershell
Test-Path -LiteralPath 'E:\NovaCore\build\srv01-display-gap-population'
```

Expected after cleanup: `False`.

Final disposition: **RETAINED — MANUAL CLEANUP REQUIRED**. Automatic approval review rejected the single deletion command before execution as **blocked by policy**. No retry or workaround. Non-destructive verification afterward confirmed the same400files/106,016,279bytes remain, with zero reparse points and zero Git-tracked files in the resolved bounded root. No files were disposed by this ticket. cleanup.json retains the exact inventory and result. Cleanup does not affect the engineering judgment.
