# SolAnalytical

`SolAnalytical` publishes the versioned `SolCompact-DE440Validated-v3` definition. It is an immutable compact analytical Solar model measured against DE440, not DE440 playback and not a high-precision ephemeris.

It uses a Sun-rooted hierarchy:

```text
Sun
├── Mercury
├── Venus
├── Earth
│   └── Moon
├── Mars
├── Jupiter
├── Saturn
├── Uranus
└── Neptune
```

The Solar System Barycenter and Earth-Moon Barycenter retain stable reserved IDs but are not nodes in this first gameplay hierarchy. The Moon is explicitly propagated relative to Earth. An Earth-centered view is therefore a derived observation transform, not a separate geocentric physics system.

## Time and coordinates

The opaque analytical time domain has a J2000-equivalent epoch at `SimulationInstant.Zero`, a 1:1 exact seconds mapping, and a finite engine evaluation interval of ±500 Julian years (`365.25 × 86400` seconds per year). Its coordinate metadata identifies J2000/ICRF-equivalent inertial axes. NovaCore does not interpret this as UTC, use calendars, or consult a host clock.

## Sources and conversion

The fixed ET0 osculating elements were derived offline from the pinned DE440 bundle through CSPICE N0067. Queries use geometric `J2000` states, aberration `NONE`, and a common SSB observer before parent-relative subtraction. CSPICE km and km/s values are converted once to SI. Each compact element record is converted once at immutable definition construction to the existing parent-relative Cartesian `TwoBodyTrajectory`; runtime evaluation uses the existing bounded universal-variable propagator.

Earth uses the DE440 Earth-Moon-barycenter heliocentric ET0 state as a documented approximation because this stable ten-body hierarchy does not include an EMB node. The Moon uses the DE440 Moon-minus-Earth ET0 state. The Sun is fixed at the runtime origin, so SSB-relative Sun motion is intentionally omitted. Offline tests quantify both approximations. The ±500-year interval is a supported deterministic engine range, not an astronomy-error guarantee and not JPL/Horizons precision.

Validation has two layers. Runtime tests independently convert each immutable authored element record to Cartesian position/velocity, compare it with the generated trajectory seed and exact-epoch output, and verify two-body invariants. The offline oracle separately compares direct CSPICE states at ET0, ±1 day, ±30 days, ±1, ±5, ±10, and ±25 Julian years, reporting deterministic max/RMS position and velocity residuals per body. Direct bounded propagation and generic-system evaluation remain the same architecture; no Solar-specific solver was added.

Physical gravitational parameters and radii are authoring constants from the NASA/JPL planetary physical-parameter and fact-sheet family; the catalog records the constants source/version metadata. They are immutable body properties, not a gravity implementation.

Custom, edited, or future source-backed systems use the same catalog and ephemeris-binding contracts. Any body identity, constants, hierarchy, trajectory, provenance, coverage, or authored-modification change produces a different deterministic definition hash.

The analytical model is intentionally tiny and allocation-free after warmup. It stores no time samples or kernels. Orbit overlays are resampled from the current simulation instant through this same compact runtime authority, so secular/periodic evolution and parent motion cannot leave an epoch-static visual path behind the body.

## Compact lunar correction

Version 1 preserved the DE440-grounded Moon ET0 seed but accumulated severe parent-relative phase and plane error: across the fixed ±25-year report epochs, maximum/RMS position reached `762,440,519.5 / 417,708,134.1 m` and maximum/RMS velocity reached `2,045.316 / 1,113.313 m/s`. Diagnostics identified mean/apsidal phase drift as dominant, plus ecliptic-plane nodal precession and bounded radial variation.

Version 2 keeps the same ET0 seed and universal-variable two-body propagation, then applies one generic authored secular correction: time scale `1.0070739315409438`, ecliptic-plane node rate `-19.165429687499998 deg/Julian year`, and in-plane periapsis rate `40.70390243530275 deg/Julian year`. Position and velocity are evaluated directly; velocity includes the deterministic derivative of both rotations and the time scaling.

The rates were fitted deterministically to 123 epochs spanning ±10 years: 60-day samples across ±3,600 days plus exact ±10-year endpoints. Held-out validation uses interleaved 60-day epochs offset by 30 days, the milestone diagnostic epochs, and ±25-year endpoints. Held-out RMS position fell from `535.541 Mm` to `47.947 Mm`; held-out maximum fell from `791.231 Mm` to `101.032 Mm`. The fixed report epochs now measure `77.686 / 41.569 Mm` maximum/RMS position and `135.805 / 79.248 m/s` maximum/RMS velocity.

Version 3 retains v2 exactly, then adds one generic immutable bounded-periodic catalog. A term carries one angular frequency and epoch-zero-preserving sine/`cosine - 1` amplitudes for radial distance and in-plane phase. Runtime applies phase inside the existing secular rotations, applies radial displacement along the evaluated state, and evaluates the analytical derivative of every term and moving radial basis. The contract is capped at eight components; the Moon uses seven distinct frequencies, four radial coefficient pairs and four phase coefficient pairs with the shared `31.8109540636 d` frequency stored once. This is 23 active fitted scalars in a uniform 280-byte catalog payload, not an extensible perturbation language.

Offline analysis used 3,601 two-day fit epochs over ±3,600 days and an interleaved two-day held-out grid, plus the milestone checkpoints and exact ±25-year endpoints. The dominant measured radial periods are `27.5701024021`, `27.4335777545`, `14.7650043565`, and `31.8109540636 d`; the dominant phase periods are `27.5727411945`, `27.4309655304`, `31.8109540636`, and `27.6335731415 d`. Successive radial terms explain `43.02%`, `93.49%`, `31.25%`, and `57.42%` of the then-remaining fit separation variance; phase terms explain `50.18%`, `91.46%`, `45.42%`, and `49.26%` of the then-remaining phase variance. Repeated fitting produces identical parameters and held-out hash `0x2831B4B39E68DAB9`.

At the fixed report epochs, v3 reduces Moon parent-relative maximum/RMS position from `77.686 / 41.569 Mm` to `34.077 / 10.243 Mm`, maximum/RMS velocity from `135.805 / 79.248 m/s` to `76.297 / 32.262 m/s`, and maximum separation error from `47.273 Mm` to `8.918 Mm`. Reconstructed-root maximum position is `46.578 Mm`, including the separately measured Earth-root approximation. Dense five-day horizon validation gives maximum separation error of `0.992 Mm` through 30/180 days, `1.809 Mm` through one year, `4.196 Mm` through five years, `8.943 Mm` through ten years, and `27.214 Mm` through 25 years. The corresponding maximum parent-relative position errors are `8.415`, `8.415`, `8.464`, `10.598`, `16.290`, and `85.200 Mm`. The ±25-year result improves v2 but is explicitly a visual/prediction bound, not precision navigation.

On the measured machine, v2/v3 all-body cost is `17.478 / 18.141 µs`; isolated Moon cost is `1.806 / 2.189 µs`. Both allocate zero warmed bytes. Exact-time repeatability, one-microtick continuity, direct velocity against a central numerical derivative, large direct jumps, and 50,000× path independence pass.

The navigation gate is therefore scoped. v3 is authoritative enough for visual maps, time warp, launch-to-LEO celestial context, and an initial approximate translunar-transfer gameplay layer. It is not precision truth for lunar orbit insertion, close lunar navigation, or long-horizon mission prediction. Those uses require a declared error budget and a higher-fidelity exceptional lunar layer—most likely compact Chebyshev or sampled Hermite data derived offline—while active spacecraft remain under a separate dynamics authority. `SampledHermite` was not selected for v3 because the seven-frequency analytical model materially improves every measured horizon at negligible runtime cost.

Sources:

- NAIF/JPL DE440 planetary ephemeris and CSPICE N0067
- NASA/JPL Solar System Dynamics, planetary physical-parameter data
