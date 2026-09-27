# Performance, allocation and storage

Four final actual application runs used 1280×720 windowed qualification and exercised display transitions. Each collected three editor windows of 120 samples after 64 warm-up samples, plus three 256-frame windows for each supported/powered/coast phase after warm-up. The table reports the largest window p95, not a pooled percentile. Full distributions are in `application-results.json`. No concurrent test process was intentionally run during these routes.

| Route | Editor CPU p95 ms | Editor interval p95 ms | Flight callback p95 ms: supported / powered / coast | Flight interval p95 ms: supported / powered / coast |
|---|---:|---:|---|---|
| Debug short | 0.0613 | 5.931 | 3.273 / 3.170 / 3.103 | 8.479 / 7.905 / 8.170 |
| Debug long | 0.0592 | 5.978 | 3.127 / 2.988 / 2.901 | 8.078 / 7.980 / 7.261 |
| Release short | 0.0342 | 5.879 | 2.515 / 0.499 / 0.458 | 8.083 / 6.299 / 7.043 |
| Release long | 0.0328 | 5.873 | 2.853 / 0.488 / 0.477 | 8.184 / 6.221 / 6.263 |

Every sampled editor render boundary allocated **0 managed bytes**, with no collections. This is PresentEditor render preparation including cached draw/socket iteration; it excludes WinForms events, input/transactions, cold thumbnails and the driver. It is not a whole-application allocation assertion.

Flight measures the whole managed callback including hosted context/input, bounded physical service, canonical Solar publication, camera and render preparation. Native interval also includes presentation/scheduling, and is not a GPU timer. Allocation median is zero, but tick frames allocate: Release maxima 54,648 bytes supported, 49,376 powered, 68,192 coast. Sampled collections per Release route: short `[2,0,0]`, long `[2,1,0]`; no Gen2 observed. This does not certify allocation-free simulation. Release maximum frame interval was 21.578 ms short / 12.180 ms long; maximum callback 4.344 / 4.525 ms. Medians, p95/p99, maxima and consecutive-tail counts remain in the JSON.

Integrated windows reported rcsFrames=0. Separate held-W and full control/physical tests prove behavior, but these timing windows do **not** measure sustained active-RCS cost. No paired before/after machine benchmark or long-duration leak certification is claimed.

Whole-process one-second sampling covered startup, editor, flight and adversarial handlers, with no idle-baseline subtraction:

| Route | Peak private bytes observed | OS peak working-set bytes | Peak handles observed |
|---|---:|---:|---:|
| Debug short | 4,686,286,848 | 4,226,023,424 | 1,135 |
| Debug long | 4,503,449,600 | 4,192,792,576 | 1,128 |
| Release short | 4,542,816,256 | 4,126,797,824 | 1,129 |
| Release long | 3,469,475,840 | 3,294,072,832 | 1,130 |

These totals include the existing Earth global/regional runtime, not just editor cost. Private/handle maxima are sampled lower bounds on instantaneous peaks. Debug includes validation overhead.

No Vulkan validation error appears in the four final logs. Debug retains WARNING-Shader-OutputNotConsumed for vertex Location 11, also present in pre-revision Gate 5/6/7 logs. This is not a warning-free graphics claim. Managed builds are warning-free.

There is one renderer, shared uploaded craft mesh lease and active canonical flight owner. Thumbnails and socket/draw caches are prepared outside warm rendering. Six definitions and 14 content files remain 374,984 bytes, with six GLBs / 29 meshes / 4,440 vertices / 2,124 triangles. No proprietary art or new geometry added. `inventory.json` records retained candidate/build/log/recovery/evidence sizes as logical bytes, not allocated clusters. Generated outputs and exploratory failures are retained without cleanup.
