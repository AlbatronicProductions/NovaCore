# NovaCore current state

## Repository state

Inspect `git status --short` before edits. The committed baseline is the current `origin/main`; current uncommitted work is the offline adaptive sampler and its focused test. It must be preserved.

## Architecture

NovaCore is a deterministic high-precision simulation foundation. It has exact simulation time, reference-frame and precision systems, immutable celestial definitions, generic analytical/sampled evaluation, NCPE v2 storage and runtime reconstruction, an offline builder, and a Vulkan rendering backend. Dependency direction is official sources → `NovaCore.NaifEphemerisAdapter` → builder input → NCPE → `NcpeCelestialSystemLoader` → `CelestialSystemDefinition` → evaluator/simulation. Runtime has no CSPICE, NAIF, adapter, builder, network, or kernel dependency.

## Offline NAIF state

Ignored `external/naif/` holds DE440 and CSPICE N0067. Verified files: `de440.bsp` (119799808 bytes, `A4CE9BF9B3282BECC9F4B2AC3CEBE03A2AE7599981AABD7265FD8482FFF7C4B5`), `gm_de440.tpc` (12406, `924DDF4FB9EAD9FE8A1AA55780BCABDE40B09D00065D58226E24B68D8092F140`), `pck00010.tpc` (126143, `59468328349AA730D18BF1F8D7E86EFE6E40B75DFB921908F99321B3A7A701D2`), `naif0012.tls` (5257, `678E32BDB5A744117A467CD9601CD6B373F0E9BC9BBDE1371D5EEE39600A039B`), and CSPICE N0067 archive (36519028, `98D60B814B412FA55294AEAAEB7DAB46D849CC87A8B709FFE835D08DE17625DC`). N0067 links with MSVC 19.51. Queries use SSB/J2000/NONE/ET and CSPICE km/km/s, converted offline to SI.

SSB is exact zero. Sun, EMB, Earth, and Moon were queried at ET -86400, 0, +86400; tested parent-relative reconstruction was 0 m and 0 m/s. ET=0 Sun X was -1067706.8053809535 km / -1067706805.3809534 m. Proven constants are extraction results, not production runtime values: BODY10_GM 132712440041.27939, BODY399_GM 398600.4355070226, BODY301_GM 4902.8001184575487 km³/s².

## CSPICE lifecycle

`native/NovaCore.CSpiceShim` is narrow `__cdecl`; `CspiceSession` explicitly loads and disposes it. CSPICE uses RETURN and NULL output. Invalid target queries produce controlled `QueryFailure`, capture SHORT/LONG text, reset error state, preserve default failed state, and permit the next Sun query.

## Current uncommitted sampler work

`AdaptiveHermiteSampler.cs` uses real Moon/EMB DE440 queries over ET 0–86400, 21600-second seeds, Hermite position/velocity, probes 1/8, 3/8, 5/8, 7/8, and midpoint splitting. It measured max position 6.374644206047227 m, max velocity 0.0008818000029948403 m/s, no subdivision, hash `0xC1473707A33172A0`, and repeat identity. Current defect: output includes cached probes (21 timestamps) rather than 5 accepted knots. RMS, worst ET, and cadence search are deferred.
