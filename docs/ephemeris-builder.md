# Ephemeris builder

`tools/NovaCore.EphemerisBuilder` is an offline .NET console tool:

```powershell
dotnet run --project tools/NovaCore.EphemerisBuilder -- --synthetic output.ncpe output.manifest
```

It validates complete normalized semantic input, writes NCPE v2 through a temporary file, validates the completed artifact, and atomically replaces the destination. The optional manifest is deterministic UTF-8 key/value text; the binary artifact remains authoritative. The synthetic fixture explicitly supplies time mapping, body identity/classification/physical properties, sources, bindings, payloads, and samples.

`--self-test` builds twice, compares bytes, and performs binary round-trip validation. Future source adapters may create `NormalizedEphemerisInput`; they must remain offline and must not leak source-specific logic into runtime simulation.
