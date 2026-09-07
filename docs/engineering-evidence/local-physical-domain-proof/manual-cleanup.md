# Manual retirement of this proof's build products

Automatic approval review rejected the deletion before it executed with
“blocked by policy”; no further reason was supplied. No deletion was retried.

The reviewed output is exclusively:

`E:\NovaCore\build\local-physical-domain-proof`

It contains 33 generated CPU-tool bin/obj files, 1,436,946 logical bytes, including
copied Core/Graphics/Interop dependencies. No Vulkan runtime/shaders/assets,
production source or unique test inputs reside there. This proof's source,
project, runner, results and bit-pattern witnesses remain in this evidence folder.
The folder had no reparse points and no Git-tracked files; its exact filenames,
sizes and SHA-256 values are recorded in `scratch-manifest.json`.

After checking that its contents still match that manifest, the user may remove
only this disposable directory from PowerShell:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\build\local-physical-domain-proof' -Recurse -Force
```

This command has **not** been executed successfully by the agent. Do not apply it
to any other build directory or the retained evidence. Rerunning `run.py` will
regenerate these build products if the numerical proof needs reproduction.
