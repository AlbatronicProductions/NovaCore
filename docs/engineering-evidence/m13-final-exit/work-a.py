"""Read-only M13.5 journal analysis; writes only this worker's JSON/Markdown.

Run with Python -B. Does not import a capture/build harness or execute GPU work.
Plain JSON and lossless .json.gz are supported. No historical run is a baseline.
"""
from __future__ import annotations
import argparse
from collections import defaultdict
import gzip
import hashlib
import json
import math
from pathlib import Path
import re

HERE = Path(__file__).resolve().parent
BANK = "d4baab6940a57a46e478b98e36f5e45d1c4b558f"
CPU = ["total", "update", "fenceWait", "inspection", "hostCallback",
       "validationUpload", "record", "submit", "acquire", "present", "recreate", "unattributed"]
SPLIT = ["resetMs", "resetBarrierMs", "cullMs", "cullBarrierMs", "compactMs", "compactBarrierMs", "blockMs"]
SCALAR_FIELDS = ["frame", "generation", "total", "candidate", "cullAndPreparation",
                 "current.cullMs", "current.compactMs", "incomingFinal.blockMs",
                 "incomingPhysical", "currentPhysical", "demand", "tcsPatches", "tesInvocations", "fragments",
                 "submission.total", "submission.fenceWait", "submission.validationUpload",
                 "submission.record", "submission.submit", "completion.total", "completion.fenceWait",
                 "completion.validationUpload", "completion.acquire", "completion.present"]


def unpack(value):
    if not isinstance(value, dict) or value.get("_columnar") != 1:
        return value or []
    return [{**value["constants"], **{k: v[i] for k, v in value["columns"].items()}}
            for i in range(value["count"])]


def load(path):
    raw = path.read_bytes()
    if path.suffix == ".gz":
        raw = gzip.decompress(raw)
    return json.loads(raw), hashlib.sha256(raw).hexdigest()


def journal_label(data, path):
    # The candidate harness prefixes its output filename, not the inherited
    # in-document label. Preserve that prefix in every analysis/reference.
    name = path.name.removesuffix('.gz').removesuffix('.json')
    return name if name.startswith('candidate-') else data.get('label', name)


def evidence_role(data, path, native_matches_bank=None):
    label = journal_label(data, path)
    if data.get('evidenceRole') or label.startswith('candidate-'):
        return data.get('evidenceRole', 'implemented unbanked default candidate')
    return ('cached-key prototype' if data.get('env', {}).get('NOVACORE_EXIT_CACHED_KEYS') else
            'bank key placement / polling instrument' if 'poll' in label else
            'bank native' if native_matches_bank else 'bank semantics / private timing')


def stats(values):
    values = sorted(x for x in values if isinstance(x, (int, float))
                    and not isinstance(x, bool) and math.isfinite(x))
    n = len(values)
    if not n:
        return None
    return dict(n=n, mean=sum(values)/n, median=values[math.ceil(n*.5)-1],
                p95=values[math.ceil(n*.95)-1], p99=values[math.ceil(n*.99)-1],
                minimum=values[0], peak=values[-1],
                **{"over"+str(t): sum(x > t for x in values)
                   for t in (6.67, 6.94, 8.33, 11.11, 16.67, 40)})


def fields(line):
    result = {}
    payload = line.split(": ", 1)[-1]
    for part in payload.split(";"):
        if "=" not in part:
            continue
        k, v = (x.strip() for x in part.split("=", 1))
        if v in ("true", "false"):
            v = v == "true"
        elif re.fullmatch(r"-?\d+", v):
            v = int(v)
        elif re.fullmatch(r"-?(?:\d+\.\d*|\.\d+)(?:[eE][+-]?\d+)?", v):
            v = float(v)
        result[k] = v
    return result


def scalar_map(rows, names):
    return {k: stats(r[k] for r in rows if k in r) for k in names}


def cpu_stats(rows):
    out = scalar_map(rows, CPU)
    out["totalMinusFence"] = stats(r["total"]-r["fenceWait"] for r in rows
                                    if "total" in r and "fenceWait" in r)
    return out


def scalar_row(row):
    values=[]
    for name in SCALAR_FIELDS:
        value=row
        for key in name.split("."):
            value=value.get(key) if isinstance(value,dict) else None
        values.append(value)
    return values


def series_stats(rows):
    out = scalar_map(rows, ["total", "candidate", "cullAndPreparation", "detailed",
                            "toneMap", "background", "preSurface", "scene",
                            "materialsOverlays", "globalFill", "incomingPhysical",
                            "currentPhysical", "demand", "tcsPatches", "tesInvocations",
                            "fragments", "clippingOutput"])
    for prefix in ("current", "incomingFinal"):
        out[prefix] = scalar_map([r[prefix] for r in rows if prefix in r], SPLIT)
    out["submissionCpu"] = cpu_stats([r["submission"] for r in rows if r.get("submission")])
    out["completionCpu"] = cpu_stats([r["completion"] for r in rows if r.get("completion")])
    return out


def logged_events(data):
    current_context = None
    split = {}
    publications, markers, physical_slices = [], [], []
    for line in data.get("lines", []):
        if "Residual GPU:" in line:
            row = fields(line)
            if "recordFrame" in row and "incoming" in row:
                split[(row["recordFrame"], row["incoming"])] = row
                current_context = row.get("observedFrame")
        if "Production spherical billboard publication:" in line:
            publications.append(dict(fields(line), observedFrameContext=current_context,
                                     contextSource="preceding ResidualInspect; must match host generation"))
        if "Performance publication:" in line:
            marker = fields(line)
            if publications and publications[-1].get("generation") == marker.get("generation"):
                publications[-1].update(observedFrameContext=marker.get("frame"),
                                        contextSource="explicit Performance publication frame", marker=marker)
            else:
                markers.append(line)
        if "NCSM1 physical slice:" in line:
            physical_slices.append(fields(line))
        if any(x in line for x in ("P2S5C3 phase:", "P2S5C3 traversal", "Earth route validation:",
                                  "Earth route scenario validation:", "finest snap:",
                                  "altitude checkpoint:", "P2S5E anchored warp", "seating stability:")):
            markers.append(line)
    return split, publications, markers, physical_slices


def analyze(data, path, normal_start=None, baseline=None):
    if data.get("bank") != BANK:
        raise ValueError(f"{path.name}: journal bank is not banked M13.5")
    direction = unpack(data.get("directionalRows"))
    host = unpack(data.get("hostRows"))
    gpu = unpack(data.get("gpuRows"))
    by_host = {r["frame"]: r for r in host}
    if len(by_host) != len(host):
        raise ValueError(f"{path.name}: duplicate host-frame identity")
    by_gpu = defaultdict(list)
    for r in gpu:
        by_gpu[(r.get("frame"), r.get("total"))].append(r)
    split, publications, markers, slices = logged_events(data)
    prep = defaultdict(lambda: defaultdict(float))
    for kind, rows in data.get("preparation", {}).items():
        for r in rows:
            # Inspection precedes ++a.frame inside UpdatePlanetary. Its value is
            # still the completed GPU identity N, before host row N+1 is logged.
            prep[int(r["frame"])][kind] += r["ms"]
    aligned, rejected = [], []
    for completion in host:
        n = completion.get("gpuFrame", 0)
        if not (n > 0 and n == completion.get("geometryFrame") == completion["frame"]-1):
            rejected.append(dict(hostFrame=completion["frame"], gpuFrame=n,
                                 geometryFrame=completion.get("geometryFrame"), reason="not current completed Earth frame"))
            continue
        candidates = by_gpu.get((n, completion.get("gpuTotal")), [])
        extended = candidates[0] if candidates and all(x == candidates[0] for x in candidates) else {}
        row = dict(frame=n, generation=completion.get("gpuGeneration"),
                   total=completion["gpuTotal"], candidate=completion["gpuCandidate"],
                   cullAndPreparation=completion["gpuCullAndPreparation"],
                   tcsPatches=completion.get("tcsPatches"), tesInvocations=completion.get("tesInvocations"),
                   fragments=completion.get("fragments"), clippingOutput=completion.get("clippingOutput"),
                   submission=by_host.get(n), completion=completion,
                   extendedExact=bool(extended),
                   incomingPhysical=prep[n].get("incomingPhysicalPreparation", 0),
                   currentPhysical=prep[n].get("currentPhysicalPreparation", 0), demand=prep[n].get("demand", 0))
        row.update({k: v for k, v in extended.items() if k not in ("frame", "total", "candidate", "cullAndPreparation")})
        for flag, name in ((0, "current"), (1, "incomingFinal")):
            if (n, flag) in split:
                row[name] = split[(n, flag)]
        aligned.append(row)
    by_aligned = {r["frame"]: r for r in aligned}
    generations = []
    last = None
    for r in aligned:
        if r["generation"] != last:
            generations.append(dict(firstObservedGpuFrame=r["frame"], generation=r["generation"]))
            last = r["generation"]
    for p in publications:
        n = p["observedFrameContext"]
        h = by_host.get(n+1) if isinstance(n, int) else None
        p["firstNewOwnerHostFrame"] = n+1 if isinstance(n, int) else None
        p["contextValidated"] = bool(h and h.get("generation") == p.get("generation"))
    dynamic = journal_label(data,path).startswith(("dynamic-", "control-", "candidate-dynamic-")) or data.get("env", {}).get("NOVACORE_P2S5F_DIRECTIONAL_GRID") == "1"
    fixed = bool(direction) and not dynamic
    warm_direction = direction[-100:] if fixed else []
    if fixed and (len(warm_direction) != 100 or any(r["submittedFrame"] != r["timingFrame"] for r in warm_direction)):
        raise ValueError(f"{path.name}: fixed100 frame identity requirement failed")
    warm_ids = {r["submittedFrame"] for r in warm_direction}
    warm_aligned = [by_aligned[n] for n in sorted(warm_ids) if n in by_aligned]
    if fixed and host and len(warm_aligned) != 100:
        raise ValueError(f"{path.name}: incomplete fixed GPU/CPU completion alignment")
    limits = []
    if not host:
        limits.append("No per-frame CPU journal; aggregate production CPU pacing only, no invented startup-separated CPU distribution.")
    if not aligned and not fixed:
        limits.append("No complete GPU series; sparse printed GPU totals do not supply route percentiles or threshold counts.")
    if normal_start is None and dynamic:
        limits.append("No explicit gameplay-start boundary supplied: all valid Earth observations and all CPU frames are retained; post30 is sensitivity only, not a startup exclusion.")
    if any(not p["contextValidated"] for p in publications):
        limits.append("Some publication counts lack a validated native-frame association; observed generation transitions are reported separately.")
    if any(not r["extendedExact"] for r in aligned):
        limits.append("Some broad host GPU rows have no unique equal frame/total extended row; absent child fields are not zero-filled.")
    normal = [r for r in aligned if normal_start is not None and r["frame"] >= normal_start]
    normal_host = [r for r in host if normal_start is not None and r["frame"] >= normal_start]
    verified_publications = [p for p in publications if p["contextValidated"]]
    first_authoritative = min((p["firstNewOwnerHostFrame"] for p in verified_publications), default=None)
    after_first = [r for r in aligned if first_authoritative is not None and r["frame"] >= first_authoritative]
    after_first_host = [r for r in host if first_authoritative is not None and r["frame"] >= first_authoritative]
    initial_host = [r for r in host if first_authoritative is not None and r["frame"] < first_authoritative]
    # Contiguous recorded preparation episodes are not asserted to be jobs: a job
    # can pause or share physical-generation metadata with a subsequent job.
    episodes, active_episode = [], []
    for r in aligned:
        active = r["incomingPhysical"] > 0 or "incomingFinal" in r
        if active and (not active_episode or r["frame"] == active_episode[-1]["frame"]+1):
            active_episode.append(r)
        else:
            if active_episode:
                episodes.append(active_episode)
            active_episode = [r] if active else []
    if active_episode:
        episodes.append(active_episode)
    episode_stats = [dict(firstFrame=e[0]["frame"], lastFrame=e[-1]["frame"],
                          currentGenerations=sorted({r["generation"] for r in e}), n=len(e),
                          statistics=series_stats(e)) for e in episodes]
    neighbors = []
    for expensive in sorted(aligned, key=lambda r: r["total"], reverse=True)[:6]:
        candidates = [r for r in aligned if r["frame"] != expensive["frame"]
                      and abs(r["frame"]-expensive["frame"]) <= 12
                      and r["generation"] == expensive["generation"]]
        cheaper = min(candidates, key=lambda r: r["total"]) if candidates else None
        neighbors.append(dict(expensive=expensive, cheaperNeighbor=cheaper,
                              exactControl=False, reason="same observed current generation; camera/pupil equivalence not assumed"))
    shader_match = None
    native_deployed_match = None
    if baseline:
        deployed = baseline.get("deployment", {}).get("Release", {})
        shader_match = data.get("shaderHashes") == deployed.get("shaders")
        native_deployed_match = data.get("nativeHash") == deployed.get("native")
    readiness = ["physicalReady", "normalsReady", "cullReady", "compactReady", "tesDrawReady", "indirectValid", "fenceComplete", "atomicFrameBoundary"]
    owner = ["invalidDraws", "zeroOwner", "overlapOwner", "staleGenerationDraws"]
    bad_publications = [p for p in publications if any(p.get(k) is not True for k in readiness) or any(p.get(k) != 0 for k in owner)]
    label=journal_label(data,path)
    role=evidence_role(data,path,native_deployed_match)
    polling=[fields(s) for s in data.get('lines',[]) if 'Performance request polling:' in s]
    if fixed:polling=[r for r in polling if r.get('frame') in warm_ids]
    elif first_authoritative is not None:polling=[r for r in polling if r.get('frame',0)>=first_authoritative]
    return dict(label=label, journalInternalLabel=data.get('label'), bank=data["bank"], kind="fixed" if fixed else "dynamic/control", evidenceRole=role,
                nativeMode=data.get("nativeMode"), nativeHash=data.get("nativeHash"), managedHash=data.get("managedHash"),
                shaderHashes=data.get("shaderHashes"), shadersMatchBaseline=shader_match,
                nativeMatchesNormalDeployment=native_deployed_match, args=data.get("args"), env=data.get("env"),
                exitCode=data.get("exitCode"), errors=data.get("errors"), seconds=data.get("seconds"),
                directionCount=len(direction), hostCount=len(host), alignedCount=len(aligned), rejectedAlignment=rejected,
                fixedWindow=[min(warm_ids), max(warm_ids)] if warm_ids else None,
                fixedGpu=stats(r["gpuTotalMs"] for r in warm_direction),
                fixedDraw=stats(r["gpuDetailedDrawMs"] for r in warm_direction),
                fixedCullCompact=stats(r["gpuCullCompactMs"] for r in warm_direction),
                fixedCounters=scalar_map(warm_direction, ["generation", "level", "topologyFamily", "compactedTriangles", "tcsPatches", "refinedVertices", "fragmentInvocations", "clippingOutputPrimitives", "maximumOuterTesFactor"]),
                fixedInputSets={k: sorted({str(r[k]) for r in warm_direction if k in r}) for k in ["cameraBody", "bodyOrientation", "viewProjection", "innerFactorBins"]},
                fixedProfile=series_stats(warm_aligned), allEarth=series_stats(aligned),
                allCpu=cpu_stats(host), normalStartFrame=normal_start, normal=series_stats(normal), normalCpu=cpu_stats(normal_host),
                firstAuthoritativeHostFrame=first_authoritative, afterFirstPublication=series_stats(after_first),
                afterFirstPublicationCpu=cpu_stats(after_first_host), beforeFirstPublicationCpu=cpu_stats(initial_host),
                afterFirstPublicationOutliers16=[r for r in after_first_host if r["total"]>16.67],
                post30Sensitivity=series_stats([r for r in aligned if r["frame"] > 30]),
                quietCurrentNoLoggedPreparation=series_stats([r for r in aligned if not any(r[k] for k in ("incomingPhysical", "currentPhysical", "demand")) and "incomingFinal" not in r]),
                publications=publications, publicationCount=len(publications), replacementPublicationCount=max(0,len(publications)-1),
                invalidPublicationRecords=bad_publications, observedGenerationTransitions=generations,
                incomingPreparationEpisodes=episode_stats, physicalSlices=slices, routeMarkers=markers,
                requestPolling=scalar_map(polling,['completeMs','queueMs','requests','uploads','pending','records']),
                cpuOutliers40=[r for r in host if r["total"] > 40], worstAndNeighbors=neighbors,
                alignedScalarRows=[scalar_row(r) for r in aligned], limits=limits)


def triplet(s):
    return "unavailable" if not s else " / ".join(f"{s[k]:.5f}" for k in ("median", "p95", "p99"))


def render(report):
    lines = ["# Work A: fresh banked M13.5 whole-frame map", "",
             f"Authoritative bank: `{BANK}`. This worker performs no builds, GPU runs or production writes.", "",
             "## Measurement boundary", "",
             "Only fresh journals based on this bank are included. Bank-native measurements, private timing, the subsequent cached-key prototype and the implemented unbanked default candidate are labeled separately; a bank field is source provenance, not proof every private control or candidate is the production baseline. Candidate filenames override inherited unprefixed internal labels. Quiet fixed GPU and separate profile CPU rows remain distinct runs. No M13.4 or pre-banking candidate measurements are substituted. Percentiles use nearest rank; threshold counts are strict greater-than.", "",
             "GPU/geometry/completion joins require positive gpuFrame == geometryFrame == host.frame - 1. Child GPU scopes additionally require exact frame and total equality. Inspection precedes ++a.frame in UpdatePlanetary: preparation log frame N and split recordFrame N both identify GPU N, completed during host frame N+1. Publication marker N introduces the new owner in host/draw N+1. CPU submission and completion components are separate.", "",
             "Do not add candidate/draw children or compute children to their enclosing spans. No raster parity, exact per-stage material time, cache behavior or calibrated publication latency follows from these timings.", "",
             "## Fresh fixed runs", "",
             "| Run | Mode | GPU median / P95 / P99 | Draw median | Completion CPU median / P95 / P99 | >8.33 / >11.11 |", "|---|---|---|---:|---|---|"]
    for r in report["runs"]:
        if r["kind"] != "fixed":
            continue
        g=r["fixedGpu"]; cpu=r["fixedProfile"]["completionCpu"]["total"]
        lines.append(f"| {r['label']} | {r['evidenceRole']} | {triplet(g)} | {r['fixedDraw']['median']:.5f} | {triplet(cpu)} | {g['over8.33']} / {g['over11.11']} |")
    lines += ["", "## Dynamic/control coverage", "", "| Run | Aligned Earth frames | GPU median / P95 / P99 / peak | >8.33 / >11.11 | Publications / replacements | Exit / logged errors |", "|---|---:|---|---|---|---|"]
    for r in report["runs"]:
        if r["kind"] == "fixed":
            continue
        g=r["allEarth"]["total"]
        tail=f"{triplet(g)} / {g['peak']:.5f}" if g else "unavailable (sparse normal telemetry)"
        counts=f"{g['over8.33']} / {g['over11.11']}" if g else "unavailable"
        lines.append(f"| {r['label']} | {r['alignedCount']} | {tail} | {counts} | {r['publicationCount']} / {r['replacementPublicationCount']} | {r['exitCode']} / {len(r['errors'] or [])} |")
    lines += ["", "## CPU critical path and steady work", "",
              "Profile warm100 medians below are the submitted frame's components. Update contains fence/inspection/callback/upload; these are not independent additive columns. Whole-frame-minus-fence is descriptive non-fence wall work, not proven removable CPU computation.", "",
              "| Profile pose | CPU total / update | Fence / non-fence | Inspection / callback / validation-upload | Record / submit / acquire / present |", "|---|---|---|---|---|"]
    for r in report["runs"]:
        if r["kind"] != "fixed" or not r["fixedProfile"]["submissionCpu"]["total"]:
            continue
        c=r["fixedProfile"]["submissionCpu"]
        val=lambda keys: " / ".join(f"{c[k]['median']:.4f}" for k in keys)
        lines.append(f"| {r['label']} | {val(['total','update'])} | {val(['fenceWait','totalMinusFence'])} | {val(['inspection','hostCallback','validationUpload'])} | {val(['record','submit','acquire','present'])} |")
    lines += ["", "The fresh mixed validation/upload scope is nearly constant across cheap and expensive fixed poses. The subsequent poll-* nested timer identifies QueueProductionRequests at about1.9 ms per frame even with zero new requests/pending work and126 pinned records/uploads. The cached-key prototype changes only backing memory for that CPU-read key role and is separately labeled above. Verify B owns its net CPU/pacing and run-order challenge; broad-scope subtraction alone does not prove recovery, and no total GPU saving is inferred.", "",
              "## Recurring dynamic cost and publication boundary", "",
              "The explicit publication marker records the just-completed GPU identity N before UpdatePlanetary increments it. Host/draw N+1 uses the new owner. The first validated publication provides an exact initial-authority boundary; it is not asserted that every pre-boundary CPU cost is intrinsically startup-only on later replacements. All later similar costs remain in the table and retained outlier rows.", "",
              "| Profile route | First authoritative host | CPU after first publication median / P95 / P99 / peak | >16.67 / >40 | Current cull / compact median |", "|---|---:|---|---|---|"]
    for r in report["runs"]:
        if r["kind"] == "fixed" or not r["afterFirstPublicationCpu"]["total"]:
            continue
        c=r["afterFirstPublicationCpu"]["total"];s=r["allEarth"]["current"]
        timings=" / ".join(f"{s[k]['median']:.5f}" if s[k] else "unavailable" for k in ['cullMs','compactMs'])
        lines.append(f"| {r['label']} | {r['firstAuthoritativeHostFrame']} | {triplet(c)} / {c['peak']:.5f} | {c['over16.67']} / {c['over40']} | {timings} |")
    lines += ["", "Contiguous incoming-preparation and final-readiness episodes, full GPU/CPU children, complete publication records and worst/same-generation-neighbor rows are retained in JSON. Regional repeats are reported in full; prior-ticket64 frame sets are not imported. The one-milestone continuation bar requires one safe, measurable >=1.5 ms net responsibility, not a collection of smaller costs.", ""]
    implemented=[r for r in report['runs'] if r['evidenceRole']=='implemented unbanked default candidate']
    if implemented:
        lines += ['## Implemented candidate boundary','',
                  'The candidate-* rows are the default implementation of the CPU-read terrain-request key memory preference. They are additional measurements and do not replace the fresh bank or same-binary prototype/return controls. Verify B contains the independent net-benefit judgment, exact-output comparisons and run-order challenge. There is no GPU8.33/120 FPS closure claim: the final Florida fixed population and recurring regional work remain over target.','',
                  'The final dynamic profiles use the normally rebuilt candidate managed application; their managed hash differs from the bank and matches candidate-deployment.json. Their shaders and route arguments match the reference. Full, regional and warp publication identity/cadence match the original bank profiles, but dynamic TCS/TES/fragment observations differ. Timing joins do not establish exact camera/physical/raster parity.']
        for r in implemented:
            for event in r['afterFirstPublicationOutliers16']:
                if event['total']>40:
                    lines += ['',f"Retained post-authority outlier: {r['label']} host{event['frame']} generation{event['generation']}, CPU{event['total']:.4f} ms, fence{event['fenceWait']:.4f} ms, completed GPU{event['gpuFrame']} total{event['gpuTotal']:.5f} ms. Its exact wait cause is unclassified. It is not silently excluded as startup-only, and it prevents a blanket assertion that every post-authority frame stays below40 ms."]
    lines += ["",
              "## Interpretation and remaining proof", "",
              "Startup/gameplay separation is accepted only from an explicit supplied boundary or the fixed100 warm window. All CPU outliers and complete aligned dynamic frames remain retained. Post30 is a sensitivity population and is not permission to discard early gameplay stalls. Incoming contiguous preparation episodes are not automatically distinct replacement jobs.", "",
              "Publication log counts are exact log counts; replacements exclude the first publication. A publication frame associated through the preceding split-timer context is used only after host-generation confirmation. Sparse normal telemetry cannot establish full GPU distributions. Same-generation expensive/cheap neighbors are not assumed to have the same camera or pupil.", ""]
    for r in report["runs"]:
        for limit in r["limits"]:
            lines.append(f"- {r['label']}: {limit}")
    if not report["runs"]:
        lines += ["No completed fresh measurements were available when this analyzer was prepared. Closure, a production winner and measured residual costs are not yet classified."]
    lines += ["", "The one justified additional responsibility is the narrowly timed CPU-read key-memory preference discussed above; its implemented-candidate performance judgment is in Verify B. No further exploratory broadening is recommended. Prior noise replacement, reuse, validator or M13.5 GPU working-data controls are not a new production baseline or a second optimization.", "",
              "## Reproduction", "", "Run `python -B docs/engineering-evidence/m13-final-exit/work-a.py` after journals are complete. It reads only fresh-bank journals and writes only work-a.json/work-a.md. An explicitly proven gameplay boundary may be supplied with `--normal-start label=FRAME`; it is recorded in the output, not silently inferred. Plain and lossless gzip journals are interchangeable.", ""]
    return "\n".join(lines)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("inputs", nargs="*", type=Path)
    parser.add_argument("--normal-start", action="append", default=[])
    args=parser.parse_args()
    starts=dict(x.rsplit("=",1) for x in args.normal_start)
    baseline=load(HERE/"baseline.json")[0] if (HERE/"baseline.json").exists() else None
    inputs=args.inputs or sorted(HERE.glob("*.json"))+sorted(HERE.glob("*.json.gz"))
    runs=[]; skipped=[]; seen=set()
    for path in inputs:
        if path.name.startswith(("work-a.","work-b.","work-c.","verify-")):
            continue
        data,digest=load(path)
        if not isinstance(data,dict) or "nativeHash" not in data or "bank" not in data:
            continue
        if data.get("bank") != BANK:
            raise ValueError(f"Refusing historical baseline: {path}")
        if 'capture' in data.get('label','').lower() or data.get('env',{}).get('NOVACORE_PIXEL_PARITY'):
            skipped.append(dict(path=str(path),reason="capture-enabled correctness journal excluded from performance statistics"));continue
        if digest in seen:
            skipped.append(dict(path=str(path),reason="byte-identical decoded journal"));continue
        seen.add(digest)
        label=journal_label(data,path)
        row=analyze(data,path,int(starts[label]) if label in starts else data.get("analysisNormalStartFrame"),baseline)
        row.update(source=path.name,decodedSha256=digest)
        runs.append(row)
    report=dict(bank=BANK,methodVersion=4,alignedScalarFields=SCALAR_FIELDS,normalStartOverrides=starts,runs=runs,duplicatesNotCounted=skipped,
                detailRecovery="Complete source journal + recorded frame; no repeated embedded host/GPU row archive")
    (HERE/"work-a.json").write_text(json.dumps(report,separators=(",",":"),allow_nan=False)+"\n",encoding="utf-8")
    (HERE/"work-a.md").write_text(render(report),encoding="utf-8")
    print(f"Analyzed {len(runs)} fresh journals; wrote work-a.json/work-a.md only.")


if __name__ == "__main__":
    main()
