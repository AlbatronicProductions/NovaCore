# Reviewed disposable output

All new build/bin/obj/generated-source/raw-output files are confined to the
reviewed parent E:\NovaCore\build\powered-contact-allocation-recheck. No production
asset, permanent test or active deployment depends on it. The parent resolves
inside E:\NovaCore\build and has no reparse points. Reproduction scripts, hashes,
raw allocation counts, native comparison and full concise process/tail results
are retained outside scratch.

| Exact path | Files before cleanup | Bytes before cleanup | Reason / reproduction source |
|---|---:|---:|---|
| E:\NovaCore\build\powered-contact-allocation-recheck\allocation | 38 | 1,631,804 | Generated active source, project, bin/obj and raw output; recheck.ps1 |
| E:\NovaCore\build\powered-contact-allocation-recheck\native | 36 | 1,761,744 | Derived witness, project, bin/obj and raw output; native.ps1 + original off-com-8.json |
| E:\NovaCore\build\powered-contact-allocation-recheck\timing | 41 | 1,753,218 | Derived types, project, bin/obj, build logs and raw three-process output; timing.ps1 + Timing.cs |
| Parent total | 115 | 5,146,766 | Fully reproducible disposable output |

Previously reviewed E:\NovaCore\build\powered-contact-event-allocation-closure
is already absent at this ticket's closeout inventory: 0 files /0 bytes. No
deletion attempt or disappearance cause is attributed to this ticket.

Cleanup status: one automatic removal attempt was rejected before execution as
"blocked by policy". No retry, alternate shell or workaround. The new root remains:
115 files /5,146,766 bytes. Manual cleanup is required; engineering judgment is
unchanged. The exact execution disposition is also recorded in identity.json.

Exact copy/paste-ready manual cleanup, limited to the new reviewed root and safe
when that root is already absent:

```powershell
if (Test-Path -LiteralPath 'E:\NovaCore\build\powered-contact-allocation-recheck') {
    Remove-Item -LiteralPath 'E:\NovaCore\build\powered-contact-allocation-recheck' -Recurse -Force
}
```

Non-destructive verification; False means no new scratch remains:

```powershell
Test-Path -LiteralPath 'E:\NovaCore\build\powered-contact-allocation-recheck'
```

No wildcard paths, old-scratch deletion retry or alternate cleanup mechanism.
