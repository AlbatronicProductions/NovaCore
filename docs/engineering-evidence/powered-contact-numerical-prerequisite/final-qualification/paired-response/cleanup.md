> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Exact disposable cleanup

Only two new disposable trees were created by this ticket. They were reviewed as bounded children of this evidence directory, with no reparse points and no pre-existing candidate/source files. Reproduction source/results are retained outside them.

| Exact path | Files | Bytes | Disposition |
|---|---:|---:|---|
| `E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\paired-response\bin` | 18 | 3,068,030 | DISPOSE; retained pending manual cleanup |
| `E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\paired-response\obj` | 44 | 1,226,790 | DISPOSE; retained pending manual cleanup |
| Total | 62 | 4,294,820 | No files deleted |

Automatic approval review rejected the one attempted deletion before execution as **blocked by policy**. No retry or workaround was attempted. Old previously blocked scratch in other investigation directories was not touched.

Exact reviewed copy/paste-ready PowerShell command, only these two paths:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\paired-response\bin','E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\paired-response\obj' -Recurse -Force
```

Non-destructive verification, before or after cleanup:

```powershell
@(
  'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\paired-response\bin'
  'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\paired-response\obj'
) | ForEach-Object {
  $exists = Test-Path -LiteralPath $_
  $files = if ($exists) { @(Get-ChildItem -LiteralPath $_ -Recurse -File -Force) } else { @() }
  [pscustomobject]@{
    Path = $_
    Exists = $exists
    Files = $files.Count
    Bytes = [long](($files | Measure-Object -Property Length -Sum).Sum)
  }
} | Format-List
```

Expected after successful manual cleanup: Exists=False, Files=0, Bytes=0 for both. Do not broaden this command to the parent evidence directory or any candidate source.
