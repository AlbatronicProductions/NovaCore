# Disposable accounting

This architecture ticket created no build/bin/obj/scratch output. New disposable: 0 files / 0 bytes. Retained analytical script/results are reproduction evidence, not disposable. Existing KSA decompilation and all previous evidence are untouched.

The two previously reported paired-response output directories are NOW ABSENT. Current Test-Path and file enumeration supersede the preceding ticket's retained counts; this ticket did not delete them and does not infer who removed them. Their reproducible outputs would be recreated by the retained paired-response csproj/source and reproduce.ps1. Their parent contains retained evidence and must never be selected for cleanup.

| Exact path | Files | Bytes | Classification |
|---|---:|---:|---|
| E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\paired-response\bin | 0 | 0 | ABSENT (formerly 18 / 3068030) |
| E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\paired-response\obj | 0 | 0 | ABSENT (formerly 44 / 1226790) |
| Total | 0 | 0 | No deletion this ticket |

Automatic cleanup: NOT ATTEMPTED this ticket. The preceding ticket's one deletion attempt was blocked by automatic approval review. No retry/workaround is made. Removed: 0. Existing older scratch is outside this ticket's cleanup scope and is not relabeled or deleted. Cleanup is not an engineering acceptance blocker.

No manual cleanup is currently necessary for these absent paths. The exact previously reviewed command is preserved below for reproduction-output accounting, NOT an instruction to retry deletion of absent paths:

~~~powershell
Remove-Item -LiteralPath 'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\paired-response\bin','E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\paired-response\obj' -Recurse -Force -ErrorAction Stop
~~~

Non-destructive verification:

~~~powershell
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
~~~

Expected after manual cleanup: Exists=False, Files=0, Bytes=0 for each. Verify retained source/evidence hashes and Git status afterward. Do not extend the command to parent evidence or unrelated scratch.
