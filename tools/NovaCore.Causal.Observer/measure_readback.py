"""Read-only measurement of retained causal records; never launches a renderer."""
import base64, json, math, struct, sys
from pathlib import Path

def statistics(rows):
    if not rows:
        return {"count": 0, "medianMs": None, "p95Ms": None, "p99Ms": None, "maximumMs": None, "worstCases": []}
    values = sorted(r[1] for r in rows)
    percentile = lambda p: values[max(0, math.ceil(len(values) * p) - 1)]
    return {"count": len(values), "medianMs": percentile(.5), "p95Ms": percentile(.95), "p99Ms": percentile(.99),
            "maximumMs": values[-1], "worstCases": [{"frame": f, "milliseconds": v} for f, v in sorted(rows, key=lambda r: r[1], reverse=True)[:10]]}

def measure(directory):
    directory = Path(directory)
    blob = (directory / "breadcrumbs.bin").read_bytes()
    header = struct.unpack_from("<16Q", blob)
    assert header[0] == 0x314c41535541434e and header[1:3] == (2, 512)
    assert 1 <= header[15] <= 131072 and len(blob) == 128 + header[15] * 512
    tail = directory / 'termination-tail.bin'
    if tail.exists():
        extra = tail.read_bytes()
        th = struct.unpack_from('<16Q', extra)
        assert th[0:3] == header[0:3] and th[5] == header[5] and 1 <= th[15] <= 16384
        assert len(extra) == 128 + th[15] * 512
        blob += extra[128:]
    records = []
    for offset in range(128, len(blob), 512):
        data = blob[offset:offset+512]
        w = struct.unpack("<64Q", data)
        if not w[0]:
            continue
        checksum = 14695981039346656037
        for i, b in enumerate(data[:504]):
            if 488 <= i < 496:
                continue
            checksum = ((checksum ^ b) * 1099511628211) & ((1 << 64)-1)
        assert w[0] == w[63] and checksum == w[61], "Corrupt causal record"
        records.append(w)
    records.sort()
    assert all(b[0] == a[0]+1 for a, b in zip(records, records[1:])), "Record gap"
    doubles = lambda x: struct.unpack("<d", struct.pack("<Q", x))[0]
    opened, operations, scheduled, transfers, complete_cpu, record_cpu, gpu_frames, writes, budgets = {}, {}, {}, [], [], [], [], {}, []
    for w in records:
        phase, kind, d = w[5], w[6], w[9:]
        if kind == 0:
            opened[phase] = w[1]
        elif kind == 1 and phase in opened:
            operations.setdefault(phase, []).append((w[2], (w[1]-opened.pop(phase))*1000/header[5]))
        if phase == 27 and kind == 2:
            if d[0] == 2:
                scheduled[d[2]] = {"identity": d[1], "bytes": d[3], "generation": d[4], "pupil": d[5]}
            elif d[0] == 6:
                transfers.append((d[2], doubles(d[4])))
                complete_cpu.append((d[2], doubles(d[5])))
            elif d[0] == 7:
                record_cpu.append((d[1], doubles(d[3]), d[2]))
            elif d[0] == 5 and d[1] and d[1] not in writes:
                writes[d[1]] = {"frame": d[2], "wallMs": d[5]/1000, "cpuMs": d[6]/10000}
        if phase == 14 and kind == 2 and d[0] == 7:
            gpu_frames.append((d[1], doubles(d[2])))
        if phase == 20 and kind == 2:
            budgets.append({"heap": d[0], "usage": d[1], "budget": d[2], "headroom": d[2]-d[1]})
    summary = json.loads((directory / "summary.json").read_text(encoding="utf-8-sig"))
    topology=[]
    proof=directory/'topology-witnesses.json'
    if proof.exists():
        for witness in json.loads(proof.read_text())['history']:
            authority=witness['Authority']
            decode=lambda key:struct.unpack('<64Q',base64.b64decode(witness[key])) if witness.get(key) else None
            before,after,timing=decode('FenceBegin'),decode('FenceEnd'),decode('FrameTiming')
            topology.append(dict(identity=authority[0],sourceHandle=authority[1],sourceIncarnation=authority[2],topologyHash=authority[3],family=authority[4],vertices=authority[5],triangles=authority[6],bytes=authority[9],frame=authority[13],submission=authority[15],completed=authority[16],completedSubmission=authority[17],gpuTransferMs=doubles(authority[20]) if witness.get('Complete') else None,cpuCompletionMs=doubles(authority[21]) if witness.get('Complete') else None,cpuRecordingMs=witness['RecordMilliseconds'],fenceMs=(after[1]-before[1])*1000/header[5] if before and after else None,fullGpuFrameMs=doubles(timing[11]) if timing else None,retired=witness.get('Retire') is not None))
    topo_stat=lambda field:statistics([(x['frame'],x[field]) for x in topology if x[field] is not None])
    return {"scope": "Retained measurements only; zero samples mean unmeasured, not zero cost", "percentiles": "nearest rank; ten worst occurrences retained; small samples do not establish tail stability",
            "topologyWitnesses":topology,"topologyGpuTransfer":topo_stat('gpuTransferMs'),"topologyCpuRecording":topo_stat('cpuRecordingMs'),"topologyCpuCompletion":topo_stat('cpuCompletionMs'),"topologyFenceWait":topo_stat('fenceMs'),"topologyFullGpuFrame":topo_stat('fullGpuFrameMs'),
            "records": len(records), "retainedSeconds": (records[-1][1]-records[0][1])/header[5] if records else 0,
            "renderedFramesSubmitted": summary["submitted"], "renderedFramesCompleted": summary["completed"], "scheduledCaptures": len(scheduled),
            "gpuCaptureFrames": statistics([r for r in gpu_frames if r[0] in scheduled]),
            "gpuOtherFrames": statistics([r for r in gpu_frames if r[0] not in scheduled]),
            "captureFenceWait": statistics([r for r in operations.get(24,[]) if r[0] in scheduled]),
            "otherFenceWait": statistics([r for r in operations.get(24,[]) if r[0] not in scheduled]),
            "gpuTransferAndBarrier": statistics(transfers), "cpuCaptureRecordingWall": statistics([(f,t) for f,t,i in record_cpu if i]),
            "cpuAuthorityOnlyRecordingWall": statistics([(f,t) for f,t,i in record_cpu if not i]), "cpuCompletionInspectionWall": statistics(complete_cpu),
            "workerHashWriteWall": statistics([(v["frame"],v["wallMs"]) for v in writes.values()]),
            "workerHashWriteThreadCpu": statistics([(v["frame"],v["cpuMs"]) for v in writes.values()]),
            "cpuOperationsByPhase": {str(k): statistics(v) for k,v in operations.items()}, "budgetSamples": budgets,
            "allocationHighWaterBytes": summary["peakAllocationBytes"], "finalAllocationBytes": summary["liveAllocationBytes"],
            "stopBeforeNativeSetupMilliseconds": (summary["firstQpc"]-summary["firstStopDecisionQpc"])*1000/header[5] if 0 < summary["firstStopDecisionQpc"] < summary["firstQpc"] else None,
            "nativeProgressAfterStopMilliseconds": (summary["rendererStoppedQpc"]-summary["firstStopDecisionQpc"])*1000/header[5] if summary["firstStopDecisionQpc"] else None}

if __name__ == "__main__":
    result = measure(sys.argv[1])
    Path(sys.argv[2]).write_text(json.dumps(result, indent=2)+"\n", encoding="utf-8")
    print(json.dumps({"records": result["records"], "renderedFrames": result["renderedFramesCompleted"], "captures": result["scheduledCaptures"], "allocationHighWaterBytes": result["allocationHighWaterBytes"]}))
