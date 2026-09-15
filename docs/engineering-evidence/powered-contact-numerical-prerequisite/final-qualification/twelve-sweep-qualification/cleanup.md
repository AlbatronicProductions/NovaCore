> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Exact disposable cleanup handoff

Only the following new directories are disposable. They contain rebuildable compiler outputs,
not source, references or decisive results. Generated candidate/diagnostic source is KEEP for
reproduction. No other disposable tree, temporary reporter or Python cache was created here.

| Exact path | Files | Bytes |
|---|---:|---:|
| `E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\twelve-sweep-qualification\bin` | 14 | 1,970,811 |
| `E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\twelve-sweep-qualification\obj` | 39 | 997,887 |
| Total | 53 | 2,968,698 |

Both paths were checked as children of this new evidence folder; recursive inventory found no
reparse points. No deletion was attempted during this ticket. No older blocked cleanup path
was retried. Counts describe the final inspected filesystem before manual cleanup.

## Reviewed manual cleanup command

Copy/paste in PowerShell. These are the only two authorized targets in this cleanup handoff:

```powershell
Remove-Item -LiteralPath `
  'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\twelve-sweep-qualification\bin', `
  'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\twelve-sweep-qualification\obj' `
  -Recurse -Force -ErrorAction Stop
```

## Non-destructive verification command

Run before cleanup to inspect current counts, or afterward to verify both paths are absent:

```powershell
$paths = @(
  'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\twelve-sweep-qualification\bin'
  'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\twelve-sweep-qualification\obj'
)
foreach ($path in $paths) {
  $exists = Test-Path -LiteralPath $path
  $files = @()
  if ($exists) { $files = @(Get-ChildItem -LiteralPath $path -File -Recurse -Force) }
  [pscustomobject]@{
    Path = $path
    Exists = $exists
    Files = $files.Count
    Bytes = [long](($files | Measure-Object -Property Length -Sum).Sum)
  }
}
```

Expected after successful cleanup: `Exists=False`, `Files=0`, `Bytes=0` for both rows.
Do not broaden paths or delete any parent evidence directory. Existing older disposable output
remains outside this ticket's cleanup inventory and was not modified.
