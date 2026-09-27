# Read-only reproduction and next-proof ladder

Use `E:\NovaCore`; do not launch NovaCore, run build scripts, change OS/driver settings or write inside the sealed capture. Historical passing reports are evidence, not permission to rerun their live commands.

Scripts under `E:\NovaCore\build\blackout-recurrence-analysis-20260926-1943`:

- `phase0.py` hashes the seal and captures entry source/package/Git identities. **Do not rerun in place:** it would overwrite entry measurements. Copy it into a fresh sibling analysis directory before repeating collection.
- `analyze-session.py` parses immutable text, counts retained samples/overflows, compares logged fingerprints, and reads the App MVID directly from PE/CLI metadata. It executes no NovaCore code. Copy into a fresh sibling for independent output.
- `preserve-os.ps1` and `preserve-channels.ps1` export read-only OS data to new output folders; they refuse existing destination folders. A later export is new evidence and must not replace the retained incident collection.
- `verify-final.py` compares current inputs with the entry baseline; without `--output` it prints JSON only. An explicit output must be a new file outside the sealed capture. This is preservation verification, not a GPU/production test.

For the last script:

```powershell
python E:\NovaCore\build\blackout-recurrence-analysis-20260926-1943\verify-final.py
```

Inspect terminal log lines 6734–6749 and the source call chain `NovaCoreNative.cpp` 2611→2637→2638, 2352–2354, 2862–2864. Present return differs from physical scanout. Do not derive full-frame FPS from sparse GPU timings. Batch UTC establishes flush ordering; it does not timestamp a GPU call.

## Smallest next proof

1. An operator with administrative read access inventories existing `C:\Windows\LiveKernelReports`; preserve any matching September 26 incident dump, original timestamps and SHA-256 into a new sibling. Read relevant protected DxgKrnl metadata/records if available. Do not change ACLs, enable channels, clean dumps, or alter TDR/driver/power settings. No GPU run required. This step is pending because current read access was denied.
2. If an existing dump narrows responsibility, analyze that owner and correlate timestamp/device/stack/submission evidence before any patch. Offline replay requires actual captured inputs; these sampled logs cannot supply the exact terminal frame.
3. If no relevant dump exists, first audit existing observer coverage offline against the ordinary canonical Release lifecycle, including logging loss, window suppression, callback, acquire/record/submit/fence/present, resource identity, completion and heartbeat. Recorder activation changes some buffer usage/device extensions; any proposed probe must disclose that difference. Do not build a new diagnostic framework merely because evidence is missing.
4. Only after reviewed offline evidence earns exposure: a separately authorized bounded recoverable GPU stage, then a controlled present stage if implicated, representative workload, and ordinary manual reproduction last. Preserve physical quality, density and fatal progress gates. No such run is authorized or performed by this report.

Stop on an identified causal owner; do not reopen unrelated corrected fronts. Without one, report the ceiling and return to Project Control.
