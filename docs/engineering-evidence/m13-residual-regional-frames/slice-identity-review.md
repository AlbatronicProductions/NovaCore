# Regional preparation slice and identity review

## Scope and threshold

This read-only review independently reconstructs the rejected D-mapped run and its separate full-recomputation oracle. It does not qualify or promote that private prototype. The evidence bank is M13.4, commit 047ae479b33831eae1c0dfa3f37c657a7f70148f.

The original diagnostic question concerns six recurring frames above 11.11 ms. The later user clarification makes 8.33 ms the hard closure target, while counts above 11.11 ms must still be reported. Explaining or eliminating these six frames alone cannot satisfy the new closure requirement.

| Prior D-mapped sample set | Aligned samples | Above 8.33 ms | Above 11.11 ms |
|---|---:|---:|---:|
| Original selected L15-to-L17 band | 44 | 35 | 4 |
| Expanded three-upscale slice band | 64 | 40 | 6 |
| All retained aligned Earth GPU samples | 839 | 533 | 6 |

There are 1,000 logical traversal frames, but only 839 retained rows satisfy the existing aligned Earth GPU/geometry sample rule. The last row above is not a claim of 1,000 measured Earth GPU frames. These are historical D-mapped results, not measurements of a later residual candidate.

## Frame and physical identity

All 64 expanded-band frames have exact matching camera, GPU constants, presentation, lighting, complete published/incoming frame bytes, topology hashes, generation values, cursor, active state and complete-dependency state against both A and D-mapped-oracle. This independent check compares the full retained pupil records, including pupil/snap tags; it does not merely reuse the previous analysis's reduced physical projection.

The oracle is a separate run. Its counts are joined to the timed run by the proven identical incoming generation, preparation frame and slice, never by an assumed wall-clock frame match. Source and destination generations/pupils legitimately differ. Current-authoritative source identity must not be confused with desired managed or incoming identity.

| Final logical frame | Current generation | Incoming generation | Published pupil / snap / LOD / denominator | Incoming pupil / snap / LOD / denominator |
|---|---:|---:|---|---|
| 343 | 16 | 17 | 25 / 11 / 14 / 786432 | 26 / 11 / 15 / 1572864 |
| 355 | 17 | 18 | 26 / 11 / 15 / 1572864 | 27 / 11 / 16 / 3145728 |
| 367 | 18 | 19 | 27 / 11 / 16 / 3145728 | 28 / 11 / 17 / 6291456 |
| 723 | 34 | 35 | 45 / 13 / 14 / 786432 | 46 / 13 / 15 / 1572864 |
| 735 | 35 | 36 | 46 / 13 / 15 / 1572864 | 47 / 13 / 16 / 3145728 |
| 747 | 36 | 37 | 47 / 13 / 16 / 3145728 | 48 / 13 / 17 / 6291456 |

All nine source/destination basis components and the radius match bit for bit for these six transitions. The raw complete source/destination pupil records are not identical to one another: their LOD, lattice denominator and identity differ as shown. Scale follows the L14-to-L15-to-L16-to-L17 path; it is not treated as constant. Per-vertex canonical directions still require their own exact GPU comparison.

All six incoming complete-dependency flags are true. Normal GPU output remains the current owner throughout the slice sequence. The map chooses an address candidate in that owner's immutable lattice; the shader checks radius, basis and actual source/destination canonical direction before copy. Spatial proximity or equal raw indices is insufficient.

## Every selected slice

The three tables cover all 64 expanded-band slice frames. Paired fields are first return / second return. Same and mapped are disjoint reuse counts; recompute is the unchanged full physical fallback. A dash-free zero is a measured zero. GPU milliseconds belong to D-mapped; content counts belong to its matched oracle. The final slice in each table is the over-11.11-ms pair.

### Incoming L15, generations 17 / 35

| Slice | Logical frames | GPU frames | First vertex | Count | Same | Mapped | Recompute | Preparation ms | Total GPU ms |
|---:|---|---|---:|---:|---:|---:|---:|---|---|
| 0 | 334 / 714 | 437 / 817 | 0 | 65536 | 65536 | 0 | 0 | 0.09432 / 0.09468 | 7.47256 / 8.99980 |
| 1 | 335 / 715 | 438 / 818 | 65536 | 65536 | 65536 | 0 | 0 | 0.08292 / 0.14576 | 7.51848 / 7.58776 |
| 2 | 336 / 716 | 439 / 819 | 131072 | 65536 | 65536 | 0 | 0 | 0.09212 / 0.09216 | 7.46688 / 7.46080 |
| 3 | 337 / 717 | 440 / 820 | 196608 | 65536 | 65536 | 0 | 0 | 0.08392 / 0.09244 | 7.55728 / 7.46964 |
| 4 | 338 / 718 | 441 / 821 | 262144 | 65536 | 65536 | 0 | 0 | 0.14304 / 0.09260 | 7.59268 / 7.51840 |
| 5 | 339 / 719 | 442 / 822 | 327680 | 65536 | 65536 | 0 | 0 | 0.09304 / 0.09400 | 7.46444 / 7.50020 |
| 6 | 340 / 720 | 443 / 823 | 393216 | 65536 | 65536 | 0 | 0 | 0.09404 / 0.09232 | 7.47888 / 7.47272 |
| 7 | 341 / 721 | 444 / 824 | 458752 | 65536 | 65536 | 0 | 0 | 0.09260 / 0.09300 | 7.45364 / 7.50140 |
| 8 | 342 / 722 | 445 / 825 | 524288 | 65536 | 12986 | 37538 | 15012 | 1.81688 / 1.82800 | 9.27008 / 9.22764 |
| 9 | 343 / 723 | 446 / 826 | 589824 | 57178 | 0 | 22782 | 34396 | 2.68064 / 2.64096 | 11.19128 / 11.14040 |

### Incoming L16, generations 18 / 36

| Slice | Logical frames | GPU frames | First vertex | Count | Same | Mapped | Recompute | Preparation ms | Total GPU ms |
|---:|---|---|---:|---:|---:|---:|---:|---|---|
| 0 | 345 / 725 | 448 / 828 | 0 | 65536 | 65536 | 0 | 0 | 0.08492 / 0.08348 | 8.31660 / 8.40152 |
| 1 | 346 / 726 | 449 / 829 | 65536 | 65536 | 65536 | 0 | 0 | 0.09408 / 0.09236 | 8.45676 / 8.23028 |
| 2 | 347 / 727 | 450 / 830 | 131072 | 65536 | 65536 | 0 | 0 | 0.09240 / 0.08400 | 8.28500 / 8.34608 |
| 3 | 348 / 728 | 451 / 831 | 196608 | 65536 | 65536 | 0 | 0 | 0.09260 / 0.09348 | 8.26008 / 8.28712 |
| 4 | 349 / 729 | 452 / 832 | 262144 | 65536 | 65536 | 0 | 0 | 0.09284 / 0.09320 | 8.39664 / 8.22228 |
| 5 | 350 / 730 | 453 / 833 | 327680 | 65536 | 65536 | 0 | 0 | 0.09224 / 0.09248 | 8.44896 / 8.44364 |
| 6 | 351 / 731 | 454 / 834 | 393216 | 65536 | 65536 | 0 | 0 | 0.09144 / 0.09316 | 8.37428 / 8.27588 |
| 7 | 352 / 732 | 455 / 835 | 458752 | 65536 | 65536 | 0 | 0 | 0.09272 / 0.09280 | 8.24792 / 8.26020 |
| 8 | 353 / 733 | 456 / 836 | 524288 | 65536 | 62394 | 2970 | 172 | 0.61772 / 0.62744 | 8.92476 / 8.94572 |
| 9 | 354 / 734 | 457 / 837 | 589824 | 65536 | 0 | 38559 | 26977 | 2.66216 / 2.65580 | 10.85488 / 10.85732 |
| 10 | 355 / 735 | 458 / 838 | 655360 | 41050 | 0 | 18791 | 22259 | 1.94696 / 1.93840 | 11.30080 / 11.30452 |

### Incoming L17, generations 19 / 37

| Slice | Logical frames | GPU frames | First vertex | Count | Same | Mapped | Recompute | Preparation ms | Total GPU ms |
|---:|---|---|---:|---:|---:|---:|---:|---|---|
| 0 | 357 / 737 | 460 / 840 | 0 | 65536 | 65536 | 0 | 0 | 0.09424 / 0.09412 | 9.11308 / 10.56676 |
| 1 | 358 / 738 | 461 / 841 | 65536 | 65536 | 65536 | 0 | 0 | 0.09280 / 0.08524 | 9.01628 / 8.95704 |
| 2 | 359 / 739 | 462 / 842 | 131072 | 65536 | 65536 | 0 | 0 | 0.09416 / 0.09364 | 9.11016 / 9.01080 |
| 3 | 360 / 740 | 463 / 843 | 196608 | 65536 | 65536 | 0 | 0 | 0.09356 / 0.09284 | 9.00748 / 9.20068 |
| 4 | 361 / 741 | 464 / 844 | 262144 | 65536 | 65536 | 0 | 0 | 0.09248 / 0.09188 | 8.98624 / 8.93736 |
| 5 | 362 / 742 | 465 / 845 | 327680 | 65536 | 65536 | 0 | 0 | 0.08416 / 0.09304 | 8.95316 / 8.98428 |
| 6 | 363 / 743 | 466 / 846 | 393216 | 65536 | 65536 | 0 | 0 | 0.09232 / 0.08464 | 9.09176 / 9.02380 |
| 7 | 364 / 744 | 467 / 847 | 458752 | 65536 | 65536 | 0 | 0 | 0.09376 / 0.09308 | 9.15300 / 8.94856 |
| 8 | 365 / 745 | 468 / 848 | 524288 | 65536 | 65536 | 0 | 0 | 0.08412 / 0.09276 | 8.97004 / 8.98792 |
| 9 | 366 / 746 | 469 / 849 | 589824 | 65536 | 53434 | 11902 | 200 | 0.72504 / 0.97908 | 9.60728 / 9.83860 |
| 10 | 367 / 747 | 470 / 850 | 655360 | 56746 | 0 | 41250 | 15496 | 1.58516 / 1.58696 | 11.65300 / 11.62920 |

## Copy, transfer and actual compute semantics

Each physical record is 64 bytes. Logical source-copy reads equal 64 times (same plus mapped); destination writes equal 64 times the full slice count. The destination is written whether a value is copied or computed. These quantities do not include lattice/map/catalog reads, physical evaluation loads, cache-line amplification or actual bus traffic.

| Slice profile | Source-copy read bytes | Destination-write bytes | Workgroups |
|---|---:|---:|---:|
| All-copy full slice | 4194304 | 4194304 | 1024 |
| L15 slice 8 | 3233536 | 4194304 | 1024 |
| L15 slice 9 | 1458048 | 3659392 | 894 |
| L16 slice 8 | 4183296 | 4194304 | 1024 |
| L16 slice 9 | 2467776 | 4194304 | 1024 |
| L16 slice 10 | 1202624 | 2627200 | 642 |
| L17 slice 9 | 4181504 | 4194304 | 1024 |
| L17 slice 10 | 2640000 | 3631744 | 887 |

One 64-thread compute dispatch performs the predicate, optional mapped lookup, record copy or full H/normal evaluation. Its retained preparation timer encloses that dispatch; it does not separately time the branches. The source copy is a shader SSBO read/write, not vkCmdCopyBuffer or a transfer-queue operation. The inspected slice path has no explicit transfer command. A transfer-engine duration is unavailable, rather than assumed equal to copy bytes.

The physical allocation journal records type 2, property flags 7 (DEVICE_LOCAL, HOST_VISIBLE, HOST_COHERENT), heap 1, for incoming physical working storage and current-pupil scratch. Those physical allocations subsequently exchange current/incoming/spare roles. Source/destination per-frame Vulkan handles were not retained, so placement evidence and the source lifetime code must not be presented as a complete allocation-identity proof. No separate physical readback mirror was introduced.

The private source map is a bounded 16 + 712106*4 = 2,848,440-byte host-visible/coherent allocation. Lattice, indices and regional payload/catalog storage keep their previous placement. The 36 diagnostic synchronous map constructions average 247.521264 ms and peak at 370.2112 ms; they are not qualified production orchestration. Their time must not be mistaken for per-slice GPU copy time or hidden by the GPU result.

## Why the largest remainder is not the sole cause

L16 slice 9 recomputes 26,977 values and takes 2.66216 / 2.65580 ms preparation, yet totals 10.85488 / 10.85732 ms. Final slice 10 recomputes fewer values (22,259) and takes less preparation time (1.94696 / 1.93840 ms), yet totals 11.30080 / 11.30452 ms. This is a direct counterexample to classifying the six failures by recomputation count alone.

| Successful to failed logical frame | Preparation delta ms | Additional (cull-and-preparation minus preparation) ms | Terrain candidate draw delta ms | TCS / TES within pair |
|---|---:|---:|---:|---|
| 342 to 343 | +0.86376 | +1.08620 | -0.02932 | 273100 / 819300 |
| 354 to 355 | -0.71520 | +1.18728 | -0.02732 | 302089 / 906267 |
| 366 to 367 | +0.86012 | +1.18808 | -0.00364 | 331531 / 994593 |
| 722 to 723 | +0.81296 | +1.08436 | +0.01408 | 273100 / 819300 |
| 734 to 735 | -0.71740 | +1.15544 | +0.00796 | 302089 / 906267 |
| 746 to 747 | +0.60788 | +1.18496 | -0.00424 | 331531 / 994593 |

The residual column is subtraction of existing nested timing scopes, not an independent barrier/cull measurement. Ordinary current terrain draw changes little, while the other compute/preparation contribution grows by approximately 1.08-1.19 ms on every failed pair.

The native RecordRegionalPreparation returns completion only after the last vertex slice. RecordProductionBillboardWork therefore runs incoming reset, camera cull, compact and their barriers only on the final slice. Current-owner cull/compact still runs every frame. This is the common correlated responsibility. These retained scopes alone cannot separate incoming cull cost, compaction, and barrier serialization or establish their avoidable portions; use the new residual timing evidence for that decision.

## Regional and facility limits

The old oracle records negative contribution counts only per completed generation. The following pairs repeat exactly:

| Generations | Regional contribution changed | Facility/support contribution changed | Basis changed | Invalid accepted |
|---|---:|---:|---:|---:|
| 17 / 35 | 20823 each | 23 each | 0 | 0 |
| 18 / 36 | 20823 each | 44 each | 0 | 0 |
| 19 / 37 | 12481 each | 5573 each | 0 | 0 |

Per-slice regional/support counts are unavailable. These counters overlap and describe changes at different spatial inputs, not mutable dataset revisions at identical directions. The old source-identity negative test disables source availability; it does not qualify stale mapped generation/address lifetime. Full original physical recomputation, including normals and support, nevertheless matches all 16 words for every admitted mapped/same-index copy in the completed oracle: 15,750,208 copies across 37 publications, zero mismatches. The 64-frame content tables do not turn that observation into a production map contract.

## Fence and publication state

The six final preparation dispatches run with the old owner retained, all incoming dependencies ready, and a frozen incoming pupil. Completion permits the final incoming reset/cull/compact work and arms the existing submitted frame fence. Only inspection after that fence performs the ownership swap.

| Final logical / GPU frame | Incoming generation | Recorded preparation-to-publication elapsed ms |
|---|---:|---:|
| 343 / 446 | 17 | 107.263900 |
| 355 / 458 | 18 | 124.651400 |
| 367 / 470 | 19 | 129.878700 |
| 723 / 826 | 35 | 109.431600 |
| 735 / 838 | 36 | 124.820100 |
| 747 / 850 | 37 | 131.268600 |

These are wall-clock preparation-job durations, not GPU synchronization timestamps or complete request latency including synchronous map construction. The old Prep publication line's pupil field is the outgoing regionalPublishedPupil at the pre-swap inspection point; the incoming identity must be obtained from the recorded incoming frame, as in the identity table above. Proximity to publication does not establish a CPU swap cost on the GPU.

## Evidence and reproduction

Inputs are retained losslessly under ../m13-regional-preparation-convergence/:

- D-mapped.json.gz: timed mapped private prototype.
- D-mapped-oracle.json.gz: full-computation physical oracle and per-slice content.
- A.json.gz: matched control history.
- analysis.json and analyze.py: original interpretation, independently checked here.
- composition.py, mapped.py, mapped.inl, private-host-v3.patch: reproducible private instrumentation and its limits.

Source ownership locations: native/NovaCore.Native/RegionalPhysicalPreparation.inl, RegionalPhysicalResidency.inl and RecordProductionBillboardWork / InspectProductionBillboardPublication in NovaCoreNative.cpp. This review creates only this document; no code, build, GPU run or capture.

Compressed journal SHA-256 values:

- A.json.gz: cf47eebff7e0cba0cd9d20e29cc8b942da4087c038100188edb06fca2a835359
- D-mapped.json.gz: 1d7633c9bde1b7c368cdaa63ece7b348d00c0dff730d2f64b8b1db6dba3569c9
- D-mapped-oracle.json.gz: 05b6e9b6349d2df17328c4d86e825b92dbc8060281292423e36f46369ffe8716

Independent check: a standalone Python parser, without importing analyze.py, decoded the gzip journals, reconstructed logical/native/GPU joins, compared full recorded identity fields against both control and oracle, verified all slice count sums, and verified the six failed-frame identities. All checks passed.

To reproduce from the repository root, pass the following block to Python standard input with the command python -B -. The -B option prevents bytecode artifacts. The block reads the journals and prints every selected slice; it writes no output files.

~~~python
import gzip, json, pathlib
P = pathlib.Path("docs/engineering-evidence/m13-regional-preparation-convergence")
def load(name):
    return json.loads(gzip.decompress((P / (name + ".json.gz")).read_bytes()))
def fields(line):
    return dict(p.strip().split("=", 1) for p in line.split(": ", 1)[1].split(";") if "=" in p)
def unpack(v):
    if isinstance(v, list): return v
    return [dict(v["constants"], **{k: c[i] for k, c in v["columns"].items()}) for i in range(v["count"])]
def correlate(d):
    logical = 0
    native, state = {}, {}
    for line in d["lines"]:
        if "Composition route frame:" in line:
            logical = int(fields(line)["logicalFrame"])
        elif "Composition frame:" in line and logical:
            x = fields(line)
            native[int(x["frame"])], state[logical] = logical, x
    render = {native[x["gpuFrame"]]: x for x in unpack(d["hostRows"])
              if x["gpuFrame"] in native and x["geometryFrame"] == x["gpuFrame"]}
    return state, render
d, o = load("D-mapped"), load("D-mapped-oracle")
state, render = correlate(d)
keys = ("cameraBits", "gpuBits", "presentationBits", "lightingBits",
        "publishedBits", "incomingBits", "currentTopology", "incomingTopology",
        "generation", "incomingGeneration", "cursor", "active", "completeDependencies")
selected = [*range(334,344), *range(345,356), *range(357,368),
            *range(714,724), *range(725,736), *range(737,748)]
for control in (load("A"), o):
    other, _ = correlate(control)
    assert all(state[n][k] == other[n][k] for n in selected for k in keys)
content = {}
for line in o["lines"]:
    if "Composition mapped oracle:" in line:
        x = fields(line)
        content[int(x["generation"]), int(x["slice"])] = x
prep = {int(x["frame"]): x["ms"] for x in d["preparation"]["incomingPhysicalPreparation"]}
for n in selected:
    x, s = render[n], state[n]
    c = content[int(s["incomingGeneration"]), int(s["cursor"]) // 65536]
    same, mapped, recompute, count = (int(c[k]) for k in ("sameIndex", "mapped", "fallback", "total"))
    assert same + mapped + recompute == count
    print(n, x["gpuFrame"], s["generation"], s["incomingGeneration"], s["cursor"],
          count, same, mapped, recompute, (same + mapped)*64, count*64,
          prep[x["gpuFrame"]], x["gpuTotal"])
assert [n for n,x in render.items() if x["gpuTotal"] > 11.11] == [343,355,367,723,735,747]
print("64 slices checked; six old-threshold failures; identity checks PASS")
~~~

The conclusion is limited: the largest physical recomputation remainder is not the sole explanation for the old six-frame failure. Final incoming work outside that dispatch is the common measured residual requiring decomposition. The 8.33-ms closure gate remains a separate, stricter acceptance requirement for any later candidate.

