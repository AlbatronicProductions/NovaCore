"""Snapshot the unbanked AD4 editor/time presentation evidence; does not qualify it."""
from pathlib import Path
import datetime
import hashlib
import json
import subprocess

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).parent
EVIDENCE = ROOT / "build/ad4-editor-restoration"

def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT).decode("utf-8").strip()

def record(path):
    path = Path(path)
    return {"path": str(path), "bytes": path.stat().st_size,
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}

scope = ["tools/NovaCore.ConstructionEditor", "tools/NovaCore.App",
         "samples/NovaCore.Triangle", "src/NovaCore.Interop",
         "native/NovaCore.Native", "tests/NovaCore.Player.Tests",
         "tests/NovaCore.Graphics.Tests"]
names = git("ls-files", "--cached", "--others", "--exclude-standard", "--", *scope).splitlines()
source = [ROOT / name for name in sorted(set(names))
          if Path(name).suffix in {".cs", ".csproj", ".cpp", ".h", ".inl", ".vert", ".frag", ".manifest"}
          or name.endswith("CMakeLists.txt")]
craft = Path("C:/Users/Tyler/Documents/NovaCore/Craft/AD4 Editor Route 20260929.craft.json")
craft_data = json.loads(craft.read_text(encoding="utf-8"))
integration = json.loads((EVIDENCE / "construction-final.json").read_text())
binary_root = ROOT / "tools/NovaCore.App/bin/Release/net10.0-windows"
payload = {
    "schema": "novacore.ad4-editor-evidence/1",
    "recordedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "judgment": "BOUNDED_ENGINEERING_AND_OBSERVED_ROUTE_PASS",
    "projectControlPlayerAcceptance": False,
    "head": git("rev-parse", "HEAD"),
    "branch": git("branch", "--show-current"),
    "indexEmpty": not bool(git("diff", "--cached", "--name-only")),
    "banked": False,
    "scopeNote": "Source snapshot includes retained AD4 changes in these modules; it is not a claim that all files changed in this correction.",
    "sources": [record(p) for p in source if p.is_file()],
    "deletedSources": [str(p.relative_to(ROOT)) for p in source if not p.exists()],
    "binaries": [record(binary_root / name) for name in
                 ["NovaCore.exe", "NovaCore.dll", "NovaCore.ConstructionEditor.dll", "NovaCore.Triangle.dll", "NovaCore.Native.dll"]
                 if (binary_root / name).is_file()],
    "evidence": [record(p) for p in sorted(EVIDENCE.iterdir()) if p.suffix == ".jpg" or p.name in
                 {"construction-final.json", "construction-final.stdout.log", "construction-final.stderr.log",
                  "build.log", "player-tests.log", "solar-test.log", "editor-regressions.log",
                  "ordinary-final.stdout.log", "ordinary-final.stderr.log"}],
    "integration": integration,
    "ordinaryRoute": {"process": 21304, "window": 8850602, "qualificationSwitches": False,
                      "extent": [3440, 1440], "dpi": 96, "exitCode": 0,
                      "constructionSaveReloadLaunchReturn": True,
                      "explorationSpeedSequence": [1, 2, 1], "physicalFlightRate": 1},
    "savedCraft": {**record(craft), "instances": len(craft_data["instances"]),
                   "connections": len(craft_data["connections"]), "revision": craft_data["revision"]},
    "report": record(OUT / "editor-restoration.md"),
    "limits": ["No new physical monitor DPI transition qualification",
               "Hardware held-key/focus-loss permutations remain separate from synthetic checks",
               "Final visual/player acceptance belongs to Project Control",
               "Existing same-day authenticated KSA history reused; final refresh remained at login",
               "Broad Graphics suite cancelled; only filtered case claimed PASS"]
}
assert payload["indexEmpty"]
assert integration["checks"] == 675 and integration["judgment"] == "APPLICATION_INTEGRATION_PASS"
assert craft_data["revision"] == 8 and len(craft_data["instances"]) == 12
native = next(x for x in payload["binaries"] if x["path"].endswith("NovaCore.Native.dll"))
assert native["sha256"] == integration["native"]
(OUT / "editor-restoration-receipt.json").write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"sources": len(payload["sources"]), "evidence": len(payload["evidence"]),
                  "head": payload["head"], "indexEmpty": payload["indexEmpty"],
                  "native": native["sha256"], "craft": payload["savedCraft"]}, indent=2))
