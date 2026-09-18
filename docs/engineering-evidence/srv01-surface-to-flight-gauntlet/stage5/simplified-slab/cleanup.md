# Exact disposable-output disposition

No source, assets, canonical terrain, old campaign output or historical evidence is in these targets.

| Exact path | Files | Bytes | Disposition |
|---|---:|---:|---|
| E:\NovaCore\build\srv01-stage5-simplified-slab | 1,872 | 442,861,426 | Results consolidated; disposable. One automatic deletion attempt was rejected before execution as blocked by policy. No retry. |
| E:\NovaCore\samples\NovaCore.Triangle\bin\Release\net10.0 | 79 | 76,647,549 | KEEP until pending manual acceptance; checked uninstrumented runtime closure. Rebuildable afterward. |
| E:\NovaCore\tools\NovaCore.Launcher\bin\Release\net10.0-windows | 9 | 1,302,998 | KEEP until pending manual acceptance; checked GUI launcher closure. Rebuildable afterward. |

Removed:0 files/0 bytes. Remaining:1,960 files/520,811,973 bytes. No obj output was created outside the named build root. Empty parent bin/Release directories need no deletion. Pre-existing disposable trees remain outside this review.

## Reviewed manual cleanup — build/observer scratch

The two prepared manual launch routes do not depend on this build root. All decisive results, failures, source/binary hashes and reproduction scripts are retained beside this file.

~~~powershell
Remove-Item -LiteralPath 'E:\NovaCore\build\srv01-stage5-simplified-slab' -Recurse -Force -ErrorAction Stop
~~~

## Reviewed manual cleanup — only after manual acceptance no longer needs these binaries

~~~powershell
Remove-Item -LiteralPath 'E:\NovaCore\samples\NovaCore.Triangle\bin\Release\net10.0','E:\NovaCore\tools\NovaCore.Launcher\bin\Release\net10.0-windows' -Recurse -Force -ErrorAction Stop
~~~

## Non-destructive verification

~~~powershell
'E:\NovaCore\build\srv01-stage5-simplified-slab','E:\NovaCore\samples\NovaCore.Triangle\bin\Release\net10.0','E:\NovaCore\tools\NovaCore.Launcher\bin\Release\net10.0-windows' | ForEach-Object {
    [pscustomobject]@{Path=$_;Exists=Test-Path -LiteralPath $_}
}
~~~

Expected False only for paths actually cleaned. Do not rerun deletion for absent paths. No broader wildcard or unrelated directory is approved by this record.
