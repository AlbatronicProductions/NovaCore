# Exact disposable inventory

No deletion attempted in this ticket. Prior two-root deletion rejection was not retried.
Current filesystem truth: both older powered-contact-lifecycle-correction and
powered-contact-final-median-closure build roots are ABSENT (0 files/0 bytes). This ticket
does not attribute their removal to its own actions and must not delete absent paths.

New output:

| Exact path | Files | Bytes |
|---|---:|---:|
| E:\NovaCore\build\powered-contact-final-qualification | 1,479 | 316,448,918 |

Count includes saved manual stdout/stderr; the manual window is closed.
The root contains solution bin/obj, copied runtime assets/native binaries, and validation
logs. Manual observation is accepted and concise evidence is retained, so this root is
disposable. No source or repository dependency bytes are targets.

Exact reviewed manual cleanup command:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\build\powered-contact-final-qualification' -Recurse -Force
```

Non-destructive verification:

```powershell
$target='E:\NovaCore\build\powered-contact-final-qualification'
$files=@(if(Test-Path -LiteralPath $target){Get-ChildItem -LiteralPath $target -File -Recurse})
[pscustomobject]@{
    Path=$target
    Exists=(Test-Path -LiteralPath $target)
    Files=$files.Count
    Bytes=($files | Measure-Object Length -Sum).Sum
}
```

An earlier shell-based evidence-write containing the cleanup command as text was rejected
by automatic approval review before execution. It attempted no deletion. This document was
written directly as a file edit. No deletion or deletion workaround was executed.
