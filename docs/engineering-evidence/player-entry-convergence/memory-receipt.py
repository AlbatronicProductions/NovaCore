"""Audit the retained native samples and bind the visually inspected loading image.

No GPU access or image modification. Run from any cwd after the qualification run.
"""
import csv
import json
import math
import re
import shutil
import statistics
from datetime import datetime, timezone
from pathlib import Path

out = Path(__file__).resolve().parent
root = out.parents[2]
run = root / "build/player-entry-convergence"
names = ("gpu-memory-final-native.csv", "gpu-memory-final.stdout.log", "gpu-memory-final.stderr.log",
         "gpu-memory-final.json", "native-memory-tests.log", "player-tests-v2.log", "launcher-tests.log",
         "cpu-editor-regressions.log", "cpu-surface-persistence.log", "cpu-surface-cycles.log",
         "transitions-final.json", "transitions-final.stdout.log", "transitions-final.stderr.log",
         "transitions-final-memory.csv", "transitions-final-performance.json",
         "construction-preservation-v2.json", "app-final-build.log", "gpu-memory-cancel-v2-native.csv",
         "gpu-memory-cancel-v2.stdout.log", "gpu-memory-cancel-v2.stderr.log")
evidence = out / "qualification"
evidence.mkdir(exist_ok=True)
for name in names:
    shutil.copy2(run / name, evidence / name)
for src, dest in (("configuration-final.png", "configuration.png"),
                  ("final-sequence-9.png", "loading.png"),
                  ("final-sequence-15.png", "gameplay.png")):
    shutil.copy2(run / src, evidence / dest)

rows = list(csv.DictReader((run / "gpu-memory-final-native.csv").open(newline="")))
rows = [{k: v if k in ("uuid", "luid") else int(v) for k, v in r.items()} for r in rows]
ready = [r for r in rows if r["status"] == 1]
for r in ready:
    included = [i for i in range(r["heapCount"]) if r[f"h{i}Flags"] & 1]
    assert r["localHeapMask"] == sum(1 << i for i in included)
    for aggregate, field in (("capacityBytes", "Capacity"), ("budgetBytes", "Budget"), ("usageBytes", "Usage")):
        assert r[aggregate] == sum(r[f"h{i}{field}"] for i in included)
assert len({r["uuid"] for r in ready}) == 1
selected = next(r for r in ready if r["sequence"] == 25)
proof = json.loads((run / "gpu-memory-final.json").read_text())
assert proof["gpuMemory"]["Uuid"] == selected["uuid"]
assert [f'{selected[k]/1073741824:.{n}f}' for k,n in (("capacityBytes",1),("usageBytes",2),("budgetBytes",2))] == ["16.0","0.58","11.50"]
log = (run / "gpu-memory-final.stdout.log").read_text(encoding="utf-8", errors="replace")
ui = next(line for line in log.splitlines() if "GPU_MEMORY_UI phase=loading sequence=25 " in line)
binding = next(line for line in log.splitlines() if "PLAYER_GPU_BIND" in line)
assert "matched=1" in binding
for key in ("usage", "budget", "capacity"):
    assert f"{key}={selected[key+'Bytes']} " in ui
sample_utc = re.search(r"utc=(\S+)", ui)[1]
capture_utc = (run / "final-sequence-9.txt").read_text(encoding="utf-8").splitlines()[0]
# Keep the original FILETIME integer too; datetime has only microsecond precision.
epoch = datetime(1601, 1, 1, tzinfo=timezone.utc)
capture_delta = datetime.fromisoformat(capture_utc.replace("Z", "+00:00"))-epoch
capture_ticks = ((capture_delta.days*86400+capture_delta.seconds)*1000000+capture_delta.microseconds)*10

def dist(values):
    a=sorted(values)
    return {"samples":len(a), "median":statistics.median(a), **{k:a[max(0, math.ceil(len(a)*p)-1)] for k,p in
            (("p95",.95),("p99",.99),("maximum",1))}} if a else None

phases={}
for line in log.splitlines():
    m=re.search(r"GPU_MEMORY_UI phase=(\w+) sequence=(\d+).*copyAndUiUs=([\d.]+)",line)
    if m: phases[int(m[2])] = (m[1], float(m[3]))
heap_receipt=[]
for i in range(selected["heapCount"]):
    local=bool(selected[f"h{i}Flags"] & 1)
    heap_receipt.append(dict(index=i, flags=selected[f"h{i}Flags"],
        flagsDecoded=[name for bit,name in ((1,"DEVICE_LOCAL"),(2,"MULTI_INSTANCE")) if selected[f"h{i}Flags"]&bit],
        included=local, reason="DEVICE_LOCAL heap counted once by heap index" if local else "Non-device-local heap excluded",
        capacityBytes=selected[f"h{i}Capacity"], budgetBytes=selected[f"h{i}Budget"], usageBytes=selected[f"h{i}Usage"]))
totals={k:selected[k] for k in ("capacityBytes","budgetBytes","usageBytes")}
receipt=dict(schema=1, judgment="BOUNDED_ENGINEERING_PASS; Project Control visual acceptance pending",
    backend="VK_EXT_memory_budget via vkGetPhysicalDeviceMemoryProperties2; sole authoritative backend; no DXGI",
    adapter=dict(name="AMD Radeon RX 6800 XT",vendorId=selected["vendor"],deviceId=selected["device"],
        driverVersionRaw=proof["gpuMemory"]["Driver"],uuid=selected["uuid"],luid=selected["luid"],luidValid=bool(selected["luidValid"])),
    sameNativeHandles=binding,
    sample=dict(sequence=25,utc=sample_utc,qpc=selected["qpc"],qpcFrequency=selected["frequency"],
                utcFileTime=selected["utcFileTime"],queryNanoseconds=selected["queryNs"]),
    heaps=heap_receipt, includedHeapIndices=[h["index"] for h in heap_receipt if h["included"]],
    aggregation="Sum each DEVICE_LOCAL heap index exactly once; memory types are not traversed; MULTI_INSTANCE flag does not multiply heap size.",
    aggregateBytes=totals, conversion="GiB = bytes / 1,073,741,824", divisorBytes=1073741824,
    unroundedGiB={k.replace("Bytes","GiB"):v/1073741824 for k,v in totals.items()},
    display=dict(capacity="16.0 GiB",usage="0.58 GiB",applicationBudget="11.50 GiB",
                 rounding=".NET fixed-point F1 capacity, F2 usage/budget; nearest rounding, decimal midpoint ties to even; current UI culture"),
    displayBinding=dict(image="qualification/loading.png",captureReturnedUtc=capture_utc,
        sampleAgeAtCaptureReturnMilliseconds=(capture_ticks-selected["utcFileTime"])/10000,
        uiLog=ui, method="Visually inspected PNG text matches unique nonzero loading sample25. Screenshot and accessibility sampling are asynchronous; use PNG, not stale accessibility text. Return timestamp bounds capture time, not exact scanout."),
    semantics=dict(capacity="Reported device-local physical heap capacity, not advertised decimal GB",
        budget="Driver's application allocation budget; neither free VRAM nor physical capacity",
        usage="Driver estimate for NovaCore's process on selected device-local heap; neither allocator payload nor system-wide usage",
        earlyZero="Observed before device allocations; driver reporting may lag; never a prediction of loaded world usage"),
    audit=dict(readySamples=len(ready),aggregateMismatches=0,identityChanges=0,
        initialUsageBytes=ready[0]["usageBytes"],maximumUsageBytes=max(r["usageBytes"] for r in ready)),
    overhead=dict(nativeQueryNanoseconds=dist([r["queryNs"] for r in ready]),
        phaseBreakdown={p:dict(queryNanoseconds=dist([r["queryNs"] for r in ready if phases.get(r["sequence"],("",))[0]==p]),
            pollCopyFormatLabelMicroseconds=dist([v[1] for v in phases.values() if v[0]==p])) for p in ("configuration","loading","gameplay")},
        sampleIntervalsSeconds=dist([(b["qpc"]-a["qpc"])/a["frequency"] for a,b in zip(ready,ready[1:])]),
        limits="Small bounded run; native duration is query only. UI measure includes Poll, optional evidence CSV flush, copy/format/label work; excludes final console write and later compositor paint. Not an isolated whole-frame treatment/control comparison."),
    lifecycle="One retained instance before START, no logical device before START; renderer borrows that instance/physical device. Same-thread timer/checkpoints poll at intended one-second cadence; cache-only reads. Timer stopped and renderer retired before instance destruction.",
    cadenceLimits="250ms UI timer plus real progress checkpoints; intended approximately1Hz query cadence (GetTickCount64 granularity). Synchronous preparation delays pumping. Stale (>3s) values hidden at next UI refresh; no guarantee of repaint during an uninterruptible driver call.",
    failureEvidence="16 production-owner mock checks; unsupported, missing entry, no local heaps, Ready-to-query-exception clears aggregates and estimates, recovery, balanced instance/surface teardown. 22 managed config/presentation checks include stale/failure formatting. Not hardware driver-fault injection or every admission/startup failure.",
    sourceCaptureNote="Captures precede a frame-statistics wording correction (GPU / VRAM unavailable to GPU timing unavailable) and an intentional-loading-cancellation catch. Captured successful config/loading/gameplay branches are unchanged. Current final source/binary hashes are sealed separately, not claimed as the exact pre-correction capture binary.")
(out / "memory-receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
print(json.dumps({"sample":receipt["sample"],"aggregate":totals,"heaps":heap_receipt,"overhead":receipt["overhead"]},indent=2))
