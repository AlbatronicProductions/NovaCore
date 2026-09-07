# Project Control closeout

**LEAD JUDGMENT: ESCALATE TO PROJECT CONTROL**

**WINNER: Skip procedural terrain material noise when its complete land-detail
contribution is zero.**

**TECHNICAL CLASSIFICATION: M13.4 CANDIDATE — FOLLOW-UP REQUIRED**

Exactly one bounded production correction is implemented and remains unbanked.
Independent material quality and settled/dynamic performance reviews pass. The
unchanged stock Release headless validation gate does not pass, so the complete
candidate is not ready for acceptance. No M13.4 title/tag is assigned.

## Measured accomplishment

Active refinement at RX 6800 XT, native 3440 x 1440, 100 matched warm samples:
total GPU 17.12243 to 5.95516 ms; terrain 14.49268 to 3.40748 ms. Total recovery
11.16727 ms, 65.22%. Normal native-DLL control confirms 11.17929 ms recovery.
Factor-one saves 3.61096 ms; grazing saves 4.78972 ms. Florida/orbital gains are
noise; contributing inland land is not accelerated. M13 remains open.

The one changed production file is
`native/NovaCore.Native/shaders/production_terrain_material.glsl` (7 insertions,
4 removals). All hardware tessellation stages, factors, geometry, quality and
future KSA-class displacement capacity remain unchanged. No physical authority,
residency, facility/support, lighting, launcher or gameplay change is included.

Full measurements, compiler evidence, KSA comparison, preparation/cost accounting,
candidate ranking and exact-output proof are in `README.md` and `summary.json`.
Source/runtime/asset identity is in `baseline-identity.json` and `deployment.json`.

## Validation and unresolved boundary

| Responsibility | Debug | Release |
| --- | --- | --- |
| Managed build | PASS; zero warnings/errors | PASS; zero warnings/errors |
| Native build | PASS | PASS |
| Stock Graphics headless | 79 pass / 0 fail / 0 skip | **78 pass / 1 fail / 0 skip, twice** |
| GPU, no window | 8 / 0 / 0 | 8 / 0 / 0 |
| Visible window | 4 / 0 / 0 | 4 / 0 / 0 |
| Native GPU | 2 / 0 / 0 | 2 / 0 / 0 |
| Native regional CPU | PASS | PASS |

The 15 launcher regressions, six production route probes, asset verification,
normal full L0–L17 traversal, ten pupil snaps, three scale reversals and warp
checks passed. Exact prepared geometry/oriented triangle multiset, D32/HDR/image
comparisons passed the representative and mixed-mask cases, with the documented
pre-existing one-pixel Florida inter-run variation retained explicitly.

The **Canonical SurfaceAnchor physical terrain authority** stock Release test
fails its compound precision/checksum/ENU/allocation assertion in both complete
headless runs. Immediate unchanged focused retry passed. Twenty private probes
all reported maximum authority error 0, checksum 3922222.9812172437, valid ENU and
0 allocated bytes. A private full suite that only logs those predicates after
measurement and before the unchanged assertion also passed 79/79.

Those passes do not explain the stock failures. The precise failed predicate is
unknown. Do not call this JIT, allocation, terrain or material failure without
evidence. The fragment shader is not executed by this headless test. No test,
fixture or expectation was weakened, and no unrelated correction was attempted.
The representative complete failure log is retained in `headless-failure.txt`;
both run identities/hashes are in `validation.json`.

One bounded follow-up is needed: reproduce the stock full-suite failure with
predicate/caller evidence and obtain an unmodified full-suite pass without
weakening expectations. Project Control should decide that validation
responsibility before accepting or banking this shader candidate. The candidate
does not need another material revision or GPU capture on current evidence.

The prior approximately 181 ms event, several newly measured cold fence stalls,
normal dynamic maxima and one motion-frame cull/preparation outlier remain
unclassified as documented. No hitch-free or full-featured 90 FPS claim is made.

## Cleanup and deployment

Removed the exact 164-file manifest: 213,777,532 logical bytes, including one
67,108,864-byte hard link to the original production elevation asset. Unlinking
that reference does not reclaim the asset's storage; the original asset hash
remains unchanged. Exclusive disposable logical payload is 146,668,668 bytes.
NTFS cluster allocation and ordinary rebuild churn were not separately measured.

Seventeen bounded capture writes totalled 2,152,688,968 bytes over the campaign;
one readback set was reused, rather than retaining that cumulative volume. No
raw GPU/geometry capture or private binary remains. Temporary scratch and generated
Python bytecode directories are absent. Exact retained bytes and accounting
definitions are in `closeout.json`; permanent records stay below the 10 MiB budget.
The unresolved headless failure's small report/log and reproduction recipe remain.

**POST-CLEANUP SMOKE: PASS.** The normal deployed Release Florida route completed
500 frames after deletion. Loader logs prove Khronos validation loaded. Terrain
publication was ready; no missing/overlapping/stale owner, Vulkan error or crash
occurred. All required production asset and shader identities remain valid.

Normal runtime:
`E:\NovaCore\samples\NovaCore.Triangle\bin\Release\net10.0\NovaCore.Triangle.exe`.
Launcher:
`E:\NovaCore\tools\NovaCore.Launcher\bin\Release\net10.0-windows\NovaCore.Launcher.exe`.
Florida preset: **Florida Launch Site**, resolving to
`--scene=sol --focus=earth --surface-site=florida-launch --physical-surface=m12d-natural-candidate`.

Deployed Release fragment SHA-256:
`8c59838abb3d7ee6418695ea40f044ca1f04f5c3c9216296d5cc19bba6110d6b`.
Deployed Release native SHA-256:
`74859dbb7854e92d3069e4081276eef98980945c2c9c4d4adf3a926b18cbaeca`.

## Final Git boundary

`git diff --check`: PASS; only the existing LF-to-CRLF informational warning.
HEAD/main/origin/main and M13.3 remain
`180eaf150ba5db6364e17dd48336690778f058f9`.
Branch: `codex/post-m13.3-next-performance-target`.
Staged diff is empty. No commit, push, merge or tag occurred.

```text
 M native/NovaCore.Native/shaders/production_terrain_material.glsl
?? docs/engineering-evidence/post-m13.3-next-target/
```

Stop here for Project Control. No further performance or unrelated test work is
included in this closeout.
