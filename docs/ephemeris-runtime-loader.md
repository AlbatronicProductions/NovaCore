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

The loader milestone originally used only a synthetic dataset fixture; that remains its focused reconstruction fixture. The repository now also includes the separate [offline NAIF/CSPICE adapter](naif-source-adapter.md), a [DE440-validated compact Solar definition](sol-analytical.md), and an offline-generated lunar orientation pack. These do not turn the NCPE loader into a kernel reader or runtime astronomical source adapter. Normal Solar evaluation uses the authored compact definition; NCPE v2 does not encode its analytical-Kepler payload.

The NCPE loader remains byte-only, with no runtime registry, discovery, hot reload, or file-based loading API. Runtime assemblies do not depend on CSPICE or NAIF kernels.
