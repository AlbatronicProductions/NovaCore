# Passing versus failing routes

Comparators: the September25 [native blackout route](../earth-blackout-closure/native-route-closure.md), September26 [canonical scalable-support routes](../scalable-launch-support/native-routes.json), and the immediately preceding successful ordinary session `20260926-221944-061-13884-583795c1d35f47058b6c6a9ed3f30b0e` retained in the same sealed capture.

| Responsibility | Earlier native blackout PASS | Fresh ordinary recurrence | Classification |
|---|---|---|---|
| Unified window / renderer extent |3440×1440 borderless App; 3440×1322 renderer |3440×1322 rendering; 2820×1322 construction |SAME renderer extent; exact incident OS window/compositor state unrecorded. |
| Build/source |Isolated Debug native `c0974adb18c6dca3de6fc8e0c11c11cd201dfd5a43a17f8cc0dde17786d7cf68` |Canonical Release native `8e0b9842570a40bcb9921c7d9bff536502e30a113161f3539814bfd079a84e46` |POSSIBLY CAUSAL difference; current package also passed recent native/ordinary routes. |
| Shader content |66 sealed shader hashes |65/66 byte-identical; only `triangle.frag.spv` changed with accepted slab directional lighting |Terrain/TCS/TES/cull/preparation SAME. Slab shader difference not positively implicated; current successful routes also exercise it. |
| Physical data |Global `38ec671f…`, regional `c45c6d94…`, elevation `4600bc01…` |Same startup content hashes |SAME. |
| Observer/fallback |Independent recorder, fatal/progress gates, exact captures until cutoff 19 then black-box fallback |Only sampled/overflowing text logs retained |Definite evidence difference; POSSIBLY CAUSAL timing/resource difference, not proven trigger. No retained incident fallback-state record. |
| Recorder-dependent resource creation |Active observer adds supported budget/device-fault extensions and transfer-source buffer usage |Ordinary default lacks observer activation; historical environment not retained |POSSIBLY CAUSAL difference requiring accounting before a later equivalence claim. |
| Camera/refinement |Minimum 83.095 m, retained outer-factor samples 1; final 237–280 m |Near 23.735 m, factor 64, LOD 17 |POSSIBLY CAUSAL workload difference; successful preceding ordinary session also reaches 24.10 m / factor 64. |
| Clock / player input |Fixed 1× camera replay |Startup→launch simulation advance 27,656,053.56627 s (~320 days) |POSSIBLY CAUSAL sequence; exact keys/rate/pause history unavailable. |
| Construction / launch |No modular craft launch episode in old matching route |Two 21-part / 4,008 kg launch attempts, one recorded refusal |POSSIBLY CAUSAL combination. First refusal branch positively evidenced; blackout connection absent. |
| Refusal details |No equivalent refusal |First attempt sequence 0; second readiness logs before disposing first failed craft |POSITIVE CAUSAL EVIDENCE only for execution of the clock refusal branch. Do not label both attempts refused. |
| RCS/support complexity |No matching craft |4,008 kg / 21 parts, authored footings |Same class passed recent ordinary support route. No positive support/RCS causal witness. |
| Support release |No modular release |First flight stayed sequence 0; second endpoint missing |UNKNOWN coverage. Do not claim completed departure, impact or support-release fault. |
| UI transitions / swapchain |One logged resize |Five resize/recreate logs |POSSIBLY CAUSAL combined sequence; preceding successful ordinary route had six, automated routes up to eleven. |
| Topology/pupil generations |Final generation 18 |Final completed generation 138, pupil 1108 |POSSIBLY CAUSAL transition-history difference. Latest recorded publication completed and rendering continued. |
| Timing/duration |175.013 s; 18,409 completed |Approximately 230 s rendering; last labelled completed frame 33,978 |POSSIBLY CAUSAL coverage difference. Prior successful ordinary session continued beyond frame 50,260. |
| Memory/lifetime |275 allocations created/freed; full observer accounting |Earlier component totals plus process samples; no terminal heap/handle trace |Evidence coverage differs. Runtime equivalence UNRESOLVED, not a memory-fault witness. |
| Present/display/overlays |Supervised route, host present cadence, no scanout proof |Ordinary interactions; final present/OS composition state unknown |POSSIBLY CAUSAL responsibility; no positive API/display failure record. |
| Saved craft IDs/revisions |No comparable document |Two distinct source hashes, revisions 259/260 |DIFFERENT BUT IRRELEVANT as opaque identity values alone; resulting content/lifecycle still matters. |

No item has positive causal evidence for the blackout itself. The old route and current successful witnesses rule out simplistic sufficient-trigger claims, not conditional failures.

## Separate clock-refusal witness

Incident first source `7cc69662b6a11f916dd48f5fc42fd8d7cec9c27ebe85181860ff8e48d0e622f9`, compiled `ed76e82758cb174d0f2ed181d4f9a46106f107e4198f11fc8c201bbc20a37c8c`.
Second source `6747c1321cf281a51e0d2c6d959c6f0c5d9695b6aa75ca0475c6f19291b8e6ff`, compiled `66c543c23cc68dc223661353b383d0defc7694fe0363c59853bab4671dc12acb`.
Both readiness records show epoch 871392704305760, 21 parts, 4,008 kg, main OFF, neutral controls.

`SolarSystemScene.TryPresentPhysicalEpoch` (`samples/NovaCore.Triangle/SolarSystemScene.cs:873–876`) refuses epoch regression, rate other than 1, pause, pending clock debt, or nonempty timeline. The shared diagnostic does not identify which predicate failed. Startup epoch and launch epoch differ by 27,656,053,566,270 microsecond ticks; this proves altered simulation history, not a particular player action or a blackout mechanism.

The first refusal is followed by thousands of completed GPU frames and another launch attempt. It therefore cannot be equated with an immediate GPU hang. Preserve this witness for a separately scoped CPU-only integration investigation; no correction was attempted during blackout forensics.
