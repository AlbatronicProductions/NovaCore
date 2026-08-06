# Ephemeris builder

`tools/NovaCore.EphemerisBuilder` is an offline .NET console tool:

```powershell
dotnet run --project tools/NovaCore.EphemerisBuilder -- --synthetic output.ncpe output.manifest
```

It validates normalized input, orders bodies and samples canonically, writes through a temporary file, validates the completed artifact, and atomically replaces the destination. The optional manifest is deterministic UTF-8 key/value text; the binary artifact remains authoritative.

`--self-test` builds twice, compares bytes, and performs binary round-trip validation. Future source adapters may create `NormalizedEphemerisInput`; they must remain offline and must not leak source-specific logic into runtime simulation.
