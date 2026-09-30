"""Summarize copied native timings against actual managed transition loop IDs."""
import csv
import json
import math
import sys
import statistics
from pathlib import Path

root = Path(__file__).resolve().parents[3] / "build/player-entry-convergence"
prefix = sys.argv[1] if len(sys.argv)>1 else "transitions"
proof = json.loads((root / (prefix+".json")).read_text())
rows = list(csv.DictReader((root / (prefix+"-timings.csv")).open(newline="")))

def distribution(values):
    values = sorted(v for v in values if math.isfinite(v))
    def p(fraction):
        return values[max(0, math.ceil(len(values) * fraction) - 1)]
    return dict(samples=len(values), median=statistics.median(values), p95=p(.95), p99=p(.99), maximum=values[-1]) if values else None

names = ["hidden", "pause_backdrop", "hidden_after_resume", "universe_dropdown", "editor", "return_hidden", "1920x1080_hidden", "3440x1440_restored"]
phases = []
for before, after in zip(proof["extents"], proof["extents"][1:]):
    selected = [r for r in rows if before["loop"] <= int(r["loop"]) < after["loop"]]
    # Separate transitions; the warm comparison drops the first 30 display loops.
    stable = selected[30:]
    gpu = {int(r["gpuSample"]): float(r["gpuTotal"]) for r in stable if int(r["gpuSample"]) > 0}
    phases.append(dict(
        phase=before["phase"], name=names[before["phase"]], loops=[before["loop"], after["loop"]],
        frame_work_ms=distribution(float(r["frameMs"]) for r in stable),
        frame_cadence_ms=distribution(float(r["cadenceMs"]) for r in stable),
        cpu_work_including_present_excluding_fence_ms=distribution(float(r["updateMs"])-float(r["fenceMs"])+float(r["recordMs"])+float(r["submitMs"])+float(r["presentMs"]) for r in stable),
        ui_host_callback_ms=distribution(float(r["callbackMs"]) for r in stable),
        gpu_ms=distribution(gpu.values()),
        transition_frame_ms=distribution(float(r["frameMs"]) for r in selected[:30]),
        repeating_120_loop_cadence=[distribution(float(r["cadenceMs"]) for r in stable[i:i+120]) for i in range(0, len(stable)-119, 120)],
    ))
result = dict(phases=phases, limits=["Short bounded shared desktop run; no 150 FPS qualification.", "Desktop activity is uncontrolled; original transitions run had Explorer occlusion during pause capture.", "CPU work includes Vulkan present; GPU timestamps exclude Windows compositor cost.", "Editor changes scene content and cannot isolate overlay cost."])
(root / ("performance.json" if prefix=="transitions" else prefix+"-performance.json")).write_text(json.dumps(result, indent=2))
for phase in phases:
    print(phase["name"], "cadence", phase["frame_cadence_ms"], "gpu", phase["gpu_ms"])
