# Reviewed disposable output

Exact target: `E:\NovaCore\build\srv01-surface-to-flight-stage1`.

Inventory before removal: **124 files, 23,916,409 bytes**. The only child root is
`artifacts`, containing this turn's isolated Debug managed build outputs:
`artifacts/bin` and `artifacts/obj` for Core, EphemerisFormat, Simulation and
Simulation.Tests. All twelve `.cs` files there are generated assembly attributes,
assembly information or global-usings files. Copied reference JSON/dependencies
remain in their original canonical source locations. No unique source, test,
asset or engineering result depends on this build tree.

Retained results, baseline/candidate hashes and reproduction commands are in this
stage package. Removal does not delete the unqualified candidate or prior evidence.
One automatic removal attempt was rejected **before execution** by automatic
approval review: “blocked by policy.” No retry or workaround was attempted.
The reviewed files remain pending manual Project Control cleanup unless a later
independent absence check in `closeout.json` records completion. No files removed
by this task as of that rejection.

Exact reviewed manual cleanup command (only if reproduction recreates the path):

```powershell
if (Test-Path -LiteralPath 'E:\NovaCore\build\srv01-surface-to-flight-stage1') {
    Remove-Item -LiteralPath 'E:\NovaCore\build\srv01-surface-to-flight-stage1' -Recurse -Force -ErrorAction Stop
}
```

Non-destructive verification:

```powershell
Test-Path -LiteralPath 'E:\NovaCore\build\srv01-surface-to-flight-stage1'
```

Expected after cleanup: `False`. No other build/cache/source tree is a target.
