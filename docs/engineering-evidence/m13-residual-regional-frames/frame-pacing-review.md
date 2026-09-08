# Independent residual frame-pacing review

Causal final-readiness cost reduction demonstrated; hard 8.33 ms M13 closure fails; no full-suite or shipping gate.

The hard M13 limit is 8.33 ms. The historical 11.11 ms comparison remains visible. All times are milliseconds unless otherwise marked. This review performs no GPU run and changes only this report and its numerical companion.

## Measurement identity

All five runs contain 1,000 logical frames, 1,103 native host frames and 37 publications. Strict GPU matching requires `gpuFrame == geometryFrame == host.frame - 1`. The matching extended GPU row is independently checked against that host result. This avoids the old correlation helper overwriting logical499 with a stale non-Earth alias.

All 1,000 input-event fields and all 839 hardware TCS/TES observations match A exactly in every reviewed mode. Camera, pupil, topology, generation and cursor equality are checked, not inferred from route names. Counter23 publication telemetry may be aliased; this review uses hardware TCS/TES counts only and makes no new physical-validation claim.

Target44 covers L16/L17 preparation in both returns. Expanded64 adds both L15 events: logical334-343,345-355,357-367,714-723,725-735,737-747. The all-Earth window contains 839 strictly aligned observations.

| Run / window | Mean | P95 | P99 | Peak | >8.33 | >11.11 |
|---|---:|---:|---:|---:|---:|---:|
| A / target44 | 14.257085 | 14.94804 | 16.68492 | 16.68492 | 44 | 44 |
| A / expanded64 | 13.876428 | 14.92424 | 16.68492 | 16.68492 | 64 | 64 |
| A / alignedEarth839 | 10.496176 | 13.53300 | 14.64660 | 16.68492 | 728 | 206 |
| D-mapped / target44 | 9.140630 | 11.30452 | 11.65300 | 11.65300 | 35 | 4 |
| D-mapped / expanded64 | 8.820823 | 11.30080 | 11.65300 | 11.65300 | 40 | 6 |
| D-mapped / alignedEarth839 | 7.925686 | 9.26404 | 10.78252 | 11.65300 | 533 | 6 |
| timing / target44 | 9.150178 | 11.33156 | 11.70416 | 11.70416 | 39 | 4 |
| timing / expanded64 | 8.804367 | 11.30036 | 11.70416 | 11.70416 | 43 | 6 |
| timing / alignedEarth839 | 7.944679 | 9.34300 | 10.74368 | 11.70416 | 536 | 6 |
| validation-control / target44 | 9.157521 | 11.62280 | 11.95288 | 11.95288 | 35 | 4 |
| validation-control / expanded64 | 8.815169 | 11.51456 | 11.95288 | 11.95288 | 39 | 6 |
| validation-control / alignedEarth839 | 7.930035 | 9.26420 | 10.76368 | 11.95288 | 533 | 6 |
| validation-vertices / target44 | 9.051957 | 10.69556 | 10.98624 | 10.98624 | 37 | 0 |
| validation-vertices / expanded64 | 8.730069 | 10.66576 | 10.98624 | 10.98624 | 42 | 0 |
| validation-vertices / alignedEarth839 | 7.893414 | 9.28412 | 10.63988 | 10.98624 | 531 | 0 |

## Six failures and nearest successful neighbors

Each cell gives successful predecessor / previously failed frame. They are consecutive slices of the same event with identical camera, current/incoming pupil, generation, topology and readiness. Logical343/355/367/723/735/747 correspond to native446/458/470/826/838/850. The generation being drawn is the current owner, not the next host row's newly published generation.

| Run | Logical neighbor/final | Total GPU | Preparation | Candidate | Cull + prep | Final incoming block |
|---|---|---:|---:|---:|---:|---:|
| A | 342/343 | 13.04632 / 14.73024 | 3.73844 / 3.23176 | 6.85448 / 6.67764 | 6.13468 / 7.99612 | not separately timed |
| A | 354/355 | 14.06520 / 14.86780 | 3.84292 / 2.45440 | 7.64036 / 7.19532 | 6.36768 / 7.61556 | not separately timed |
| A | 366/367 | 14.94804 / 16.68084 | 3.83464 / 3.35452 | 8.29092 / 7.87276 | 6.60004 / 8.75108 | not separately timed |
| A | 722/723 | 12.73544 / 14.53568 | 3.73940 / 3.21664 | 6.56532 / 6.48604 | 6.11320 / 7.99256 | not separately timed |
| A | 734/735 | 13.82032 / 14.92424 | 3.83880 / 2.44228 | 7.39684 / 7.24836 | 6.36644 / 7.61876 | not separately timed |
| A | 746/747 | 14.74624 / 16.68492 | 3.88944 / 3.33712 | 8.03680 / 7.88224 | 6.65244 / 8.74552 | not separately timed |
| D-mapped | 342/343 | 9.27008 / 11.19128 | 1.81688 / 2.68064 | 6.40140 / 6.37208 | 2.81128 / 4.76124 | not separately timed |
| D-mapped | 354/355 | 10.85488 / 11.30080 | 2.66216 / 1.94696 | 7.05208 / 7.02476 | 3.74556 / 4.21764 | not separately timed |
| D-mapped | 366/367 | 9.60728 / 11.65300 | 0.72504 / 1.58516 | 7.66272 / 7.65908 | 1.88684 / 3.93504 | not separately timed |
| D-mapped | 722/723 | 9.22764 / 11.14040 | 1.82800 / 2.64096 | 6.34760 / 6.36168 | 2.82212 / 4.71944 | not separately timed |
| D-mapped | 734/735 | 10.85732 / 11.30452 | 2.65580 / 1.93840 | 7.05824 / 7.06620 | 3.74152 / 4.17956 | not separately timed |
| D-mapped | 746/747 | 9.83860 / 11.62920 | 0.97908 / 1.58696 | 7.64068 / 7.63644 | 2.14024 / 3.93308 | not separately timed |
| timing | 342/343 | 8.97460 / 11.12524 | 1.58824 / 2.63060 | 6.33168 / 6.34768 | 2.58548 / 4.71916 | 1.09052 |
| timing | 354/355 | 10.77984 / 11.33156 | 2.65932 / 1.94272 | 6.97428 / 7.07120 | 3.74820 / 4.20208 | 1.1776 |
| timing | 366/367 | 9.72168 / 11.70416 | 0.71920 / 1.57692 | 7.75044 / 7.70116 | 1.91364 / 3.94396 | 1.19844 |
| timing | 722/723 | 9.32236 / 11.16372 | 1.81276 / 2.63780 | 6.45612 / 6.38196 | 2.80884 / 4.72276 | 1.08888 |
| timing | 734/735 | 10.93544 / 11.30036 | 2.65840 / 1.95136 | 7.14048 / 7.02232 | 3.73756 / 4.21852 | 1.17932 |
| timing | 746/747 | 9.87548 / 11.67396 | 0.97056 / 1.57756 | 7.68324 / 7.67728 | 2.13448 / 3.93684 | 1.19908 |
| validation-control | 342/343 | 9.26764 / 11.42748 | 1.81820 / 2.63216 | 6.39652 / 6.37024 | 2.81368 / 4.99956 | 1.37184 |
| validation-control | 354/355 | 10.76368 / 11.51456 | 2.65788 / 1.93932 | 6.97172 / 7.00020 | 3.73464 / 4.45592 | 1.43912 |
| validation-control | 366/367 | 9.89720 / 11.94520 | 0.96748 / 1.59416 | 7.70924 / 7.64592 | 2.13012 / 4.23992 | 1.48056 |
| validation-control | 722/723 | 9.01460 / 11.45584 | 1.59436 / 2.67360 | 6.36592 / 6.37148 | 2.59136 / 5.02548 | 1.35688 |
| validation-control | 734/735 | 10.79852 / 11.62280 | 2.65364 / 1.93892 | 7.01528 / 7.10132 | 3.72996 / 4.46248 | 1.445 |
| validation-control | 746/747 | 9.81392 / 11.95288 | 0.92920 / 1.58212 | 7.66292 / 7.73712 | 2.09268 / 4.15616 | 1.41132 |
| validation-vertices | 342/343 | 9.03240 / 10.19840 | 1.59732 / 2.64696 | 6.38212 / 6.38512 | 2.59284 / 3.75372 | 0.10484 |
| validation-vertices | 354/355 | 10.79456 / 10.29268 | 2.66964 / 1.94276 | 6.99160 / 7.10716 | 3.74564 / 3.12716 | 0.10784 |
| validation-vertices | 366/367 | 9.59936 / 10.66576 | 0.71432 / 1.58212 | 7.66460 / 7.74204 | 1.87660 / 2.86428 | 0.12152 |
| validation-vertices | 722/723 | 9.26448 / 10.16788 | 1.82412 / 2.64036 | 6.38704 / 6.36796 | 2.81944 / 3.74156 | 0.10568 |
| validation-vertices | 734/735 | 10.98624 / 10.28452 | 2.65808 / 1.94148 | 7.19416 / 7.09308 | 3.73412 / 3.13264 | 0.11572 |
| validation-vertices | 746/747 | 9.86456 / 10.59928 | 0.97144 / 1.57984 | 7.67140 / 7.67588 | 2.13504 / 2.86400 | 0.12364 |

Preparation is nested within cull/preparation. Candidate and background/material/scene scopes are not independent additive costs. The numerical companion retains every GPU scope and the arithmetic remainder. Transfer-engine command/byte counters are zero in the new six-frame state logs; shader copies belong inside physical preparation.

| Run / final logical | Reset | Reset barrier | Cull or validation | Cull barrier | Compact | Final barrier | Entire block |
|---|---:|---:|---:|---:|---:|---:|---:|
| timing / 343 | 0.000760 | 0.004720 | 0.721680 | 0.011440 | 0.318600 | 0.033320 | 1.090520 |
| timing / 355 | 0.000760 | 0.004720 | 0.782320 | 0.011560 | 0.345200 | 0.033040 | 1.177600 |
| timing / 367 | 0.000760 | 0.004720 | 0.796760 | 0.010840 | 0.353480 | 0.031880 | 1.198440 |
| timing / 723 | 0.000720 | 0.004720 | 0.721120 | 0.011400 | 0.318000 | 0.032920 | 1.088880 |
| timing / 735 | 0.000760 | 0.004760 | 0.783000 | 0.011360 | 0.345200 | 0.034240 | 1.179320 |
| timing / 747 | 0.000800 | 0.004760 | 0.796960 | 0.010840 | 0.354040 | 0.031680 | 1.199080 |
| validation-control / 343 | 0.000800 | 0.004720 | 1.365440 | 0.000640 | 0.000200 | 0.000040 | 1.371840 |
| validation-control / 355 | 0.000720 | 0.004720 | 1.432800 | 0.000640 | 0.000160 | 0.000080 | 1.439120 |
| validation-control / 367 | 0.000720 | 0.004720 | 1.474240 | 0.000640 | 0.000200 | 0.000040 | 1.480560 |
| validation-control / 723 | 0.000760 | 0.004720 | 1.350480 | 0.000640 | 0.000200 | 0.000080 | 1.356880 |
| validation-control / 735 | 0.000720 | 0.004720 | 1.438680 | 0.000640 | 0.000200 | 0.000040 | 1.445000 |
| validation-control / 747 | 0.000840 | 0.004720 | 1.404840 | 0.000680 | 0.000160 | 0.000080 | 1.411320 |
| validation-vertices / 343 | 0.000760 | 0.004760 | 0.091880 | 0.007200 | 0.000200 | 0.000040 | 0.104840 |
| validation-vertices / 355 | 0.000760 | 0.004720 | 0.097320 | 0.004800 | 0.000200 | 0.000040 | 0.107840 |
| validation-vertices / 367 | 0.000760 | 0.004720 | 0.110960 | 0.004840 | 0.000160 | 0.000080 | 0.121520 |
| validation-vertices / 723 | 0.000760 | 0.004720 | 0.095160 | 0.004800 | 0.000200 | 0.000040 | 0.105680 |
| validation-vertices / 735 | 0.000720 | 0.004720 | 0.105240 | 0.004800 | 0.000200 | 0.000040 | 0.115720 |
| validation-vertices / 747 | 0.000800 | 0.004720 | 0.113080 | 0.004800 | 0.000200 | 0.000040 | 0.123640 |

## Bound on the updated gate

A final-slice-only correction cannot alter the nonfinal rows. Even the optimistic arithmetic removal of the entire incoming readiness block is insufficient. It assumes no replacement work and is not a measured implementation.

| Run / window | Nonfinal >8.33 / count | Nonfinal mean / peak | Zero incoming block mean | P95 | Peak | Remaining >8.33 |
|---|---:|---:|---:|---:|---:|---:|
| timing / target44 | 35 / 40 | 8.914945 / 10.93544 | 9.042123 | 10.50572 | 10.93544 | 39 |
| timing / expanded64 | 37 / 58 | 8.537595 / 10.93544 | 8.696026 | 10.47488 | 10.93544 | 43 |
| validation-control / target44 | 31 / 40 | 8.897387 / 10.79852 | 9.026248 | 10.54156 | 10.79852 | 35 |
| validation-control / expanded64 | 33 / 58 | 8.521588 / 10.79852 | 8.682283 | 10.46464 | 10.79852 | 39 |
| validation-vertices / target44 | 33 / 40 | 8.911097 / 10.98624 | 9.041305 | 10.69556 | 10.98624 | 37 |
| validation-vertices / expanded64 | 36 / 58 | 8.560619 / 10.98624 | 8.719456 | 10.54424 | 10.98624 | 42 |

The vertex control clears the historical 11.11 ms comparison across this measured route. It does not clear 8.33 ms. This is evidence for the identified responsibility, not permission to expand into another optimization or run the full production suite.

## CPU and publication

CPU submission rows include diagnostic map/index work; completion rows include the prior GPU fence result and publication inspection. Reporting only completion rows would hide synchronous startup of replacement jobs. Update contains its four listed components; do not sum them twice. Full component statistics, including acquire and unattributed residual, are retained for every window and both alignments in the JSON.

| Run | All1000 submission mean / P95 / P99 / peak | Target44 submission mean / peak | Target44 completion mean / peak |
|---|---:|---:|---:|
| A | 11.90416 / 15.97820 / 17.31920 / 25.74950 | 16.35211 / 17.48700 | 16.81693 / 19.32940 |
| D-mapped | 19.17706 / 13.29890 / 352.28770 / 430.59210 | 44.35164 / 389.11790 | 11.77171 / 14.66780 |
| timing | 19.55989 / 13.40130 / 353.60740 / 458.20450 | 44.38823 / 388.51930 | 11.88368 / 14.70190 |
| validation-control | 19.48240 / 13.38440 / 347.57720 / 403.86160 | 45.18986 / 403.54570 | 11.79503 / 14.69880 |
| validation-vertices | 22.47898 / 13.29930 / 406.48160 / 898.46720 | 45.28071 / 401.00450 | 11.81287 / 13.82230 |

| Run | Map count / total / mean / peak | Index qualification count / total / peak | In-route index count / total |
|---|---:|---:|---:|
| A | none | none | none |
| D-mapped | 36 / 8910.76550 / 247.52126 / 370.21120 | none | none |
| timing | 36 / 8961.97650 / 248.94379 / 417.21060 | none | none |
| validation-control | 36 / 8745.90260 / 242.94174 / 352.19650 | none | none |
| validation-vertices | 36 / 9336.90070 / 259.35835 / 406.72160 | 10 / 3116.74640 / 451.43110 | 9 / 2665.31530 |

Index qualification scans occur once per distinct topology in this private revision, including one before logical traversal. Synchronous map construction remains present. These costs are not production CPU qualification or evidence of a safe asynchronous map lifetime.

| Run | Normal job publication count | Mean | P95 | Peak |
|---|---:|---:|---:|---:|
| A | 36 | 111.075522 | 187.12820 | 188.75680 |
| D-mapped | 36 | 77.549458 | 129.87870 | 131.26860 |
| timing | 36 | 78.155361 | 130.94490 | 131.18660 |
| validation-control | 36 | 77.786036 | 130.44560 | 131.39450 |
| validation-vertices | 36 | 77.405253 | 129.92700 | 134.96830 |

Publication duration excludes startup generation1 here and follows the existing preparation-job clock. It is not total demand-to-publication latency. Selected publication identities and elapsed times are preserved in the companion JSON. Aliased publication element counters are deliberately excluded from this review's proof.

## Judgment

The vertex control supports the narrow causal result and removes the six historical >11.11 ms tails in this route. The hard <=8.33 ms M13 closure gate fails across both transition windows and ordinary aligned Earth observations. No final-slice-only path can meet that gate because too many unaffected frames already exceed it. CPU orchestration and the production physical/publication proof are not qualified here. No full-suite or shipping gate is passed.

Sources and exact SHA-256 identities, frame inputs, hardware counts, CPU components and all selected samples are in [frame-pacing-review.json](frame-pacing-review.json). All source journals remain the measurement authority.

## Reproduction after lossless compression

Run the following with `python -B` from the repository root. It accepts either journal form and checks the uncompressed JSON hash. It prints the GPU and CPU statistics, all six matched scope rows, publication timing, and map/index-scan timing without rebuilding or running the renderer. The fixed GPU-row positional relationship is asserted against each strict host result before using its scopes.

```python
import pathlib, json, gzip, hashlib, math, statistics
root = pathlib.Path.cwd()
review = json.loads((root / "docs/engineering-evidence/m13-residual-regional-frames/frame-pacing-review.json").read_text())
def unpack(v):
    return [{**v["constants"], **{k: x[i] for k, x in v["columns"].items()}} for i in range(v["count"])] if isinstance(v, dict) and v.get("_columnar") == 1 else v
def fields(s):
    return dict(x.strip().split("=", 1) for x in s.split(": ", 1)[1].split(";") if "=" in x)
def stats(v):
    v = sorted(v)
    if not v: return None
    q = lambda p: v[min(len(v)-1, math.ceil(p*len(v))-1)]
    return dict(count=len(v), mean=statistics.mean(v), median=statistics.median(v), p95=q(.95), p99=q(.99), peak=v[-1], minimum=v[0], total=sum(v), countAbove8_33=sum(x>8.33 for x in v), countAbove11_11=sum(x>11.11 for x in v))
components = ["update", "fenceWait", "inspection", "hostCallback", "validationUpload", "acquire", "record", "submit", "present", "recreate", "total", "unattributed"]
for mode, source in review["sources"].items():
    p = root / source["jsonPath"]
    raw = p.read_bytes() if p.exists() else gzip.decompress((root/source["gzipPath"]).read_bytes())
    assert hashlib.sha256(raw).hexdigest() == source["uncompressedJsonSha256"]
    data = json.loads(raw); frames = {}; journal = {}; logical = 0; blocks = {}
    for line in data["lines"]:
        if "Composition route frame:" in line: logical = int(fields(line)["logicalFrame"])
        if "Composition frame:" in line and logical:
            row = fields(line); frames[int(row["frame"])] = logical; journal[logical] = row
        if "Residual GPU:" in line:
            row = fields(line); blocks[int(row["recordFrame"]), int(row["incoming"])] = row
    host = unpack(data["hostRows"]); byhost = {r["frame"]: r for r in host}; gpu = unpack(data["gpuRows"])
    aligned = {frames[r["gpuFrame"]]: r for r in host if r["gpuFrame"] > 0 and r["gpuFrame"] in frames and r["gpuFrame"] == r["geometryFrame"] == r["frame"]-1}
    assert len(journal) == 1000 and len(aligned) == 839
    for r in aligned.values():
        g = gpu[r["frame"]-3]; assert g["frame"] == r["gpuFrame"] and g["total"] == r["gpuTotal"]
    for name, selected in {"target44": review["windows"]["target44"], "expanded64": review["windows"]["expanded64"], "alignedEarth839": sorted(aligned)}.items():
        result = dict(gpu=stats([aligned[i]["gpuTotal"] for i in selected]))
        result["cpuSubmission"] = {k: stats([byhost[int(journal[i]["frame"])][k] for i in selected]) for k in components}
        result["cpuCompletion"] = {k: stats([aligned[i][k] for i in selected]) for k in components}
        if blocks:
            result["zeroCostIncomingBlock"] = stats([aligned[i]["gpuTotal"] - float(blocks.get((aligned[i]["gpuFrame"], 1), {}).get("blockMs", 0)) for i in selected])
        print(mode, name, json.dumps(result))
    print(mode, "CPU all logical submission", json.dumps({k: stats([byhost[int(r["frame"])][k] for r in journal.values()]) for k in components}))
    for final in review["windows"]["failedSix"]:
        for i in [final-1, final]:
            h = aligned[i]; nf = h["gpuFrame"]
            print(mode, "matched", i, json.dumps(dict(identity=journal[i], gpu=gpu[h["frame"]-3], currentBlock=blocks.get((nf, 0)), incomingBlock=blocks.get((nf, 1)), tcs=h["tcsPatches"], tes=h["tesInvocations"], cpuSubmission={k: byhost[nf][k] for k in components}, cpuCompletion={k: h[k] for k in components})))
    for prefix, column in [("Prep publication:", "elapsedMs"), ("Composition map:", "diagnosticSynchronousCpuMs"), ("Residual topology qualification:", "cpuMs")]:
        rows = [fields(s) for s in data["lines"] if prefix in s]
        if prefix == "Prep publication:": rows = [r for r in rows if int(r["generation"]) != 1]
        print(mode, prefix, json.dumps(dict(stats=stats([float(r[column]) for r in rows]), rows=rows)))
```
