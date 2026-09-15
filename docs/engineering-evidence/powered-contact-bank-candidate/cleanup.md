# Disposable cleanup

Counts below were measured after this correction's successful bounded validation,
before deletion, and captured in preservation-manifest.json.

| Exact reviewed path | Files | Bytes | Disposition |
| --- | ---: | ---: | --- |
| `E:\NovaCore\build\powered-contact-attribution-correction` | 224 | 44,470,220 | DISPOSABLE: Debug/Release project artifacts; reproducible from successor source |
| `E:\NovaCore\build\powered-contact-production-integration` | 1 | 104,525 | DISPOSABLE: pinned upstream generator input; hash/URL retained in generator |
| Total | 225 | 44,574,745 | No candidate source/license/evidence inside these roots |

Exact reviewed manual cleanup command (also safe when either path is absent):

```powershell
$paths = @(
    'E:\NovaCore\build\powered-contact-attribution-correction',
    'E:\NovaCore\build\powered-contact-production-integration'
)
foreach ($path in $paths) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction Stop
    }
}
```

Non-destructive verification:

```powershell
@(
    'E:\NovaCore\build\powered-contact-attribution-correction',
    'E:\NovaCore\build\powered-contact-production-integration',
    'E:\NovaCore\build\powered-contact-final-qualification'
) | ForEach-Object {
    [pscustomobject]@{ Path = $_; Exists = Test-Path -LiteralPath $_ }
}
```

Final execution/remaining status is recorded in identity.json. The historical
final-qualification root was already absent at preflight. No other historical
research or unrelated build/data directory is included in this cleanup.

Automatic approval review blocked the single deletion attempt. No retry or
workaround occurred. The user performed the reviewed manual cleanup; independent
filesystem checks then returned False for all three listed paths. **225 files /
44,574,745 bytes removed; this ticket's disposable remaining: 0.** The commands
remain printed for exact reproducibility, not a request to delete absent paths.
