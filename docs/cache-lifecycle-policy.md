# Cache lifecycle review and bounded maintenance

**READY TO BANK CACHE LIFECYCLE PACKAGE** — unbanked candidate for Project Control.
Repository Debt Retirement remains open. Operator instructions live in
[terrain-assets.md](terrain-assets.md#cache-lifecycle-and-maintenance); this report
records the responsibility decisions, measured inventory and validation.

## Baseline

Initially clean `main`. HEAD, local `origin/main` and directly queried remote main:
`d3ecaba8d7cf716580fdaf7c8e069b7c2559a8d4`, the accepted tiny-local/internal-authoring
migration. Earlier packages `742cd8953045343310c809bec4067baf6586f2fa` and
`4cd59634ee0c6327b5d573dc375f493f7d61570a` are banked ancestors. P2S5H remains
`32ffac50ab5c06518ede24edfb5c531976d4ec99` under
`m12d-p2s5h-earth-route-convergence`. No ref/index/history changes.

Process, user and machine `NOVACORE_ASSET_CACHE` settings were unset. The active
terrain root is `E:/NovaCore/.novacore/cache/terrain/v1`. All 13 files beneath
`.novacore/cache` were SHA-256 read; all eight addressed objects match their
filename hashes. See the [compact inventory](engineering-evidence/cache-lifecycle/inventory.json).

| Root / scope | Files | Logical bytes | Allocated bytes |
|---|---:|---:|---:|
| `.novacore/cache` | 13 | 1,012,006,819 | 1,012,031,488 |
| Active `cache/terrain/v1` subset | 10 | 830,722,322 | 830,738,432 |
| `.novacore/baseline-cache` | 0 | 0 | 0 |
| `.novacore/staging` | 0 | 0 | 0 |

Initial `build/` logical accounting was 60,218,897 bytes, including the small
inventory script. The subsequent allocation-aware inventory measured 60,511,365
logical / 63,024,168 allocated bytes including temporary audit output, 1,765 files.
Six historical `earth-data` junctions were listed and excluded from traversal;
their target assets were not duplicated in accounting. File allocation uses
Windows `FileStandardInfo.AllocationSize`, not an assumed cluster size or
`GetCompressedFileSize` as a substitute for allocation. No cache-root reparse paths
were found. Build output is development/deployment state, not part of the disk
cache eviction candidate set.

## Responsibility map

Classes: A reacquirable; B regenerable; C session/transient; D incomplete/failed;
E required local authority; F insufficient evidence. Classification does not by
itself authorize eviction of a currently deployed object.

| Cache category / class | Producer → consumer | Identity, integrity, authority and recovery | Lifetime and cleanup decision |
|---|---|---|---|
| Current global and Florida final objects — **B, protected** | AssetTool build/install/fetch → managed startup verification → native bounded record reads | `sha256/xx/hash.nccube`; complete byte hash plus manifest size/structure/body/terrain/schema checks. Tracked manifests/provenance/generators and local source rasters own recovery. No artifact URLs currently configured. | Retain every current manifest's object across builds/sessions. Never selected by this cleanup; explicit build/install restores missing/corrupt objects. |
| Six historical final hash objects — **F** | Former generator/candidate runs → no current manifest consumer found; explicit path-based tools/readers can still inspect | Each self-hash verified. Historical format/terrain identities remain distinguishable; some old content reports survive, but exact supported recovery recipes/environment are not proven for all six. | Coexist indefinitely under existing storage mechanics, but not accepted permanent debt. Retain pending exact recovery/consumer review; no generic stale-hash prune added. |
| Publication copies `sha256/xx/hash.nccube.incomplete-PID-GUID` — **C while active; D after failure** | `PublishFromFile` copies verified-authority input → staged verifier → atomic final rename | Exact namespace/owner name. Normal `finally` removes own temporary. New cleanup additionally fingerprints the temporary and verifies the complete final recovery copy. | Report by default. Only old, dead-owner, unlocked plain files with intact recovery objects can be explicitly removed. Current inventory: zero. |
| `.downloads/<asset>-GUID.nccube.incomplete` — **C/D; ownership uncertain for automatic removal** | AssetTool HTTP acquisition → verification/publication | Unique GUID; HTTP response failure rejects, no automatic runtime download. Current filenames carry no owning PID/lease; download remains open exclusively while copying, but handle absence alone is not process ownership. | Producer `finally` cleanup remains. Excluded from generic cleanup; quiescent review needed after crashes. Current inventory: zero. |
| `.generation/<asset>-GUID.nccube.incomplete` and sidecars — **C/D; ownership uncertain** | AssetTool launches offline generator → verified atomic publication | Generator child may outlive its parent. Unique filenames lack durable child ownership; code cleans output and `output.manifest.json` in `finally`. | Do not infer abandonment from parent exit or age. Excluded. Two older completed JSON reports (43,925 bytes) remain **F** provenance pending review, not active incomplete files. |
| `.novacore/cache/sources/usgs-3dep/*.tif` — **E** | Explicit USGS acquisition script → Florida generator | Exact source URL/provenance and SHA `532ab3…a74cd`; local 174,745,033-byte raster verified. URL is a declared reacquisition path, not a freshly tested remote-availability guarantee. | Protect local source authority. Acquisition uses hash-before-move and own `finally` cleanup. It is independent of `NOVACORE_ASSET_CACHE`; never traversed by terrain cleanup. |
| Source download `.tif.incomplete-GUID` — **C/D, excluded** | Explicit source downloader → verified TIFF | Source SHA before publication; no PID naming. | Own process cleans normal failure; manual quiescent source recovery is separate. None present. |
| `cache/m12-level9-test.nccube` + JSON — **F** | Historical L9 regional experiment → historical investigation references only | SHA verified; metadata describes 49 L9 records and USGS source. No current production manifest selects it; exact regeneration of this historical identity not demonstrated. | Retain pending a bounded evidence/reproduction decision; 6,539,464 bytes. |
| `baseline-cache`, `.novacore/staging` — **F historical roots, empty now** | Prior investigations, not current AssetTool roots | No current runtime/default producer found. | No content to evict; do not scan them through a changed override or invent a new runtime namespace. |
| Build/native outputs and managed `bin`/`obj` — **B / active deployment** | CMake, shader compilation and MSBuild → developer tests/runtime | Source/build inputs and current configuration; sample build copies explicit shader/native outputs and removes stale deployed shaders through its existing manifest list. | Rebuildable, but protect verified deployment. Existing build tools own cleanup; this command never traverses these roots. |
| CPU/GPU resident tiles, canonical physical samples, prepared publications, orbit/frame caches — **C** | Current preparation/evaluation → rendering/query consumers | Body/terrain/physical/sample identity, bounded residency slots/queues, publication lifetime and existing invalidation. | Session/resource disposal and existing eviction remain authoritative. Disk cleanup does not alter them or become another physical owner. |

No current category is granted unconditional A eviction merely because a URL
exists. Global source images/elevation, topology assets, manifests and permanent
fixtures outside the cache remain required authority. NuGet/toolchain caches,
driver caches, Git packs and LFS objects are not NovaCore AssetTool eviction
responsibilities and were not modified.

## Identity and stale generations

The authoritative chain remains source/provenance/manifest → deterministic
bytes → cryptographic content address → full verification → runtime consumer.
`terrain/v1` is the distribution namespace, not terrain generation or NCCUBE
schema. The full artifact hash covers body/terrain/versioned record bytes and
layout, so incompatible changed bytes get different addresses. Generator identity
is provenance; two generators producing identical complete bytes may intentionally
share one object. No additional version layer is justified. SHA-256 is collision
resistant; filenames alone are never integrity evidence.

Manifest changes, terrain-4→5 migration, coordinate/payload revisions, abandoned
candidates and explicit regenerations create new hashes without deleting prior
objects. Crash paths create unique staging files. Changing an override leaves the
previous root behind; the new root is not authority to clean the old one. Age is
not a reachability test. Current production manifests reference exactly:

- Global: `38ec671f475896f2c0a674e952f4121f117b18b1446bd363e3596bada4bf47ae`,
  61,484,224 bytes, NCCUBE1 schema 1 / terrain 5.
- Florida: `c45c6d94e004e1a2927dc65d405a347b1800c619b22b2eb6b3543f3c445d3afe`,
  210,307,596 bytes, NCCUBE2 schema 3 / payload 3 / terrain 5.

Both resolve beneath the active `sha256` root and verify against the manifests in
`assets/terrain/manifests`. Generators are `build_cube_surface_pack.py` and
`build_local_terrain_pack.py`; arguments, source paths and provenance are tracked.
Florida's generator source path remains repository-relative even when the output
cache is overridden. No source relocation/acquisition redesign was made.

| Unreferenced final hash prefix | Bytes | Known lineage / remaining gate |
|---|---:|---|
| `17344f8a…` | 7,650,603 | Local schema 2/payload 1/terrain 4; old recipe/environment required |
| `288f56e1…` | 61,484,224 | Prior global terrain 5; surviving old generated report, not current manifest |
| `5e92a067…` | 61,484,224 | Global terrain 4; historical baseline references only |
| `60ada894…` | 7,652,567 | Non-regional schema 2/payload 2/terrain 5; current generic producer exists, but exact recipe reproduction not demonstrated here |
| `6f10ebcd…` | 210,307,596 | Earlier schema-3 Florida candidate; same size does not imply same content |
| `b8ba5198…` | 210,307,363 | Earlier schema-3 Florida candidate; exact source/generator environment needed |
| **Total** | **558,886,577** | **Unreferenced, not proven safely reclaimable** |

Current searches found historical reports/evidence, rather than active manifest
selection, for these hashes. Explicit generic path consumers remain possible;
historical Git references alone neither require retention forever nor prove safe
deletion. All 13 inventory file hashes are preserved in the compact evidence JSON.

## Implementation and safety contract

The old `RemoveStaleIncompleteFiles` recursively deleted any `*.incomplete*`
under its argument using age alone. A broad override could therefore include
unrelated files; age could not prove inactive writers or ownership. Its existing
API now uses the same narrowed inspection/removal policy as AssetTool.

`TerrainCacheCleanup.Inspect` enumerates only immediate lowercase two-hex
directories beneath `sha256`, then exact publication names. It reports deterministic
candidate ordering, size, temporary hash, final content identity and retained
reasons. The CLI prints the selected manifest's reference status and explicitly
states that all final objects are retained, including other manifests' objects.

`clean-incomplete` defaults to report-only. `--apply` applies the already reported
set without a second enumeration that could add candidates. Each candidate must
still match its path, bytes/hash/mtime and dead PID. PID reuse/inaccessibility
causes retention. Final content must still verify and is held open without delete
sharing. The Windows temporary is opened exclusively with DELETE access, checked
as a plain file and deleted by handle. A changed/locked/inaccessible/read-only
candidate is skipped; CLI apply returns failure if any candidate is skipped.
Non-Windows deletion is conservatively refused. No producer or runtime lock
protocol, daemon, periodic task or automatic startup eviction was added.

The policy deliberately cleans only abandoned **duplicate publication attempts**.
An incomplete download with no complete recovery copy is not selected, even if its
owner appears dead. It is not treated as authoritative rendered data either.
This small recovery-copy rule avoids asserting that an unknown old asset remains
remotely available or reproducible.

Overrides preserve existing precedence/full-path behavior. Reparse ancestors and
leaves are rejected, missing roots cause no writes, and the inspected namespace
does not broaden to siblings, parent directories, source roots or staging trees.
Unavailable locations are reported as retained; no valid final object can become
a partial object through this cleanup. Normal acquisition still verifies then
publishes through its unchanged same-volume atomic move.

## Validation

- Debug and Release solution builds PASS, both zero warnings/errors.
- Permanent cache-lifecycle fixture checks PASS in Debug/Release: report-only,
  exact set/order, current final protection, active PID, locked file, read-only
  file, same-size changed candidate, missing/corrupt recovery object, interrupted
  apply/resume, override precedence/containment and linked-root refusal. Windows
  junction fallback exercised the reparse check without symlink privileges.
- Existing asset-distribution regression PASS in both configurations; unknown
  incomplete owners are now retained intentionally. Its previous production-sized
  empty corruption fixture was replaced with a 5,032-byte synthetic case preserving
  invalid-cache rejection. New cache tests use the existing bounded global fixture.
- Actual Release CLI install → dry-run (3-byte candidate retained) → explicit
  apply (only those 3 bytes removed) → verify PASS in an isolated tiny test root.
  No-URL fetch returns its existing actionable failure; no remote download or
  production copy was required. Fixture tests also reinstall after missing and
  corrupt occupants to prove atomic recovery.
- Actual active production root dry-run: zero candidates / zero reclaimable bytes.
- Global/Florida production verify PASS with unchanged full sizes/hashes above.
- Runtime resolution, producers, manifests, shaders and accepted Earth behavior
  are unchanged. No runtime smoke or Florida campaign was needed for a maintenance-
  only path. No Graphics/window-validation or performance work was begun.
- Git whitespace/new-file checks and final scope/ref/index checks PASS.

KSA's current local `AssetManager`, `AssetBundle` and `GpuTextureSystem` were
inspected narrowly: named assets, load tracking and session resource disposal are
visible, not an equivalent cryptographic disk-generation eviction responsibility.
No equivalent cleanup policy was established. No forced ADOPT/ADAPT decision or
general format/history investigation follows, and no proprietary content is copied.

## Storage and deferred gates

Production cache before/after: **1,012,006,819 logical / 1,012,031,488 allocated
bytes**. Reclaimable proven candidates: **0**. Actually reclaimed from the real
cache: **0**. Current production objects retained: **271,791,820 logical /
271,794,176 allocated bytes**. Required local source retained: **174,745,033 bytes**.
Remaining **565,469,966 bytes** are unreferenced/historical objects and associated
reports with insufficient current eviction evidence, not an authorized deletion set.

Diagnostic output: approximately 124 MB logical transient output across the early
existing corruption regression, small fixture copies and inventory/logs. The two
early production-sized empty test files accounted for 122,968,448 logical bytes;
they were automatically removed and that test now uses only the bounded fixture.
This is not a physical disk-allocation or production-cache-reclaimed claim.
New tests automatically remove their synthetic directories. Temporary CLI copies
and audit files are removed after consolidating evidence. Permanent report plus
inventory budget: 32 KiB; no raw capture, production pack copy or new binary fixture
is retained. Normal build outputs remain current development/deployment artifacts.

Deferred cache gates are specific:

1. For each old final object, identify exact source/generator revision, arguments,
   environment and any current explicit-path consumer, then reproduce its hash or
   provide another verified recovery copy before approving eviction.
2. For historical `.downloads`/`.generation`/source temporaries, establish quiescent
   owning-process/child completion and recovery authority. A future ownership
   protocol requires a separate bounded decision; PID-only inference for a generator
   parent is insufficient.
3. Protect the local USGS source until an explicit source-eviction decision verifies
   reacquisition. Current remote availability was not tested in this ticket.

Unknown retention is conditional review debt, not permanent acceptance. Public
compatibility, native full-file probe debt, Git/LFS history, cache-residency performance
and unrelated cleanup remain outside this package. Nothing staged, committed,
pushed or tagged. Stop for Project Control review.
