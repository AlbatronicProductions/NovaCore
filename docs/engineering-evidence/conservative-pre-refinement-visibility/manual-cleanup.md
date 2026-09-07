# Manual retirement of this proof's disposable output

Automatic approval review rejected the scoped deletion command with **blocked by policy** before it executed. No automatic retry or alternate deletion mechanism was attempted.

The read-only `scratch-retirement-manifest.json` identifies every file by size and SHA-256. The disposable remainder is exactly:

- `E:\NovaCore\build\conservative-pre-refinement-visibility`: 600 temporary diagnostic files; 407,583,455 logical bytes. This includes 134,217,728 bytes of hard links to production elevation data whose original links remain in the normal deployment. Removing the scratch links does not remove the originals.
- `E:\NovaCore\docs\engineering-evidence\near-surface-performance\__pycache__\analyze.cpython-311.pyc`: one generated Python bytecode file, 8,854 logical bytes.

The final report, derivation, compact frame records, source patches, selftests, source/runtime identities, validation results and bounded interrupted-run overlay are retained in `docs/engineering-evidence/conservative-pre-refinement-visibility`. The normal Release deployment is outside the disposable directory and has been rebuilt and validated. Windows/user crash dumps remain untouched.

After Project Control review, the user may run these exact PowerShell commands manually:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\build\conservative-pre-refinement-visibility' -Recurse -Force
Remove-Item -LiteralPath 'E:\NovaCore\docs\engineering-evidence\near-surface-performance\__pycache__\analyze.cpython-311.pyc'
```

Do not delete either engineering-evidence package, the normal launcher/runtime, production assets, `.novacore`, `.git`, or any other build directory as part of this cleanup. There is no remaining diagnostic process that needs these scratch files. Retaining the empty `__pycache__` directory afterward is harmless.
