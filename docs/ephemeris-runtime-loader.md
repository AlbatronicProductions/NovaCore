# Ephemeris runtime loader

NCPE v2 is immutable storage, not runtime simulation authority. `NcpeCelestialSystemLoader` accepts artifact bytes and, only after full neutral decode, runtime validation, and definition-hash equivalence, returns one immutable `CelestialSystemDefinition`.

`NovaCore.Simulation` references only `NovaCore.EphemerisFormat`; the format assembly has no simulation dependency, and runtime code never references the offline builder. NCPE v1 is deliberately rejected with `UnsupportedV1Reconstruction` because it lacks required semantic data.

The loader defensively copies all decoded values into existing runtime contracts. Caller-owned bytes are never retained. On any failure it returns no definition and performs no publication or persistent mutation. Its gate is exact:

```text
stored expected definition hash
== neutral NCPE v2 runtime-compatible hash
== reconstructed CelestialSystemDefinition hash
```

Loading allocates immutable arrays by design. After loading, existing body/source/payload lookup, sampled interpolation, full system evaluation, and definition hashing remain allocation-free on warmed paths.

The repository contains only a synthetic dataset fixture. There is no JPL/SPICE adapter, real ephemeris dataset, runtime registry, discovery, hot reload, or file-based loading API.
