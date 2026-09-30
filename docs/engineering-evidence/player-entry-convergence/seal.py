"""Seal the current unbanked candidate and retained qualification artifacts."""
import hashlib
import json
import subprocess
from datetime import datetime, timezone
from pathlib import Path

here=Path(__file__).resolve().parent
root=here.parents[2]
def git(*args):
    return subprocess.check_output(["git", "-C", str(root), *args]).decode("utf-8").strip()
def item(p):
    return {"path":p.relative_to(root).as_posix(),"bytes":p.stat().st_size,
            "sha256":hashlib.sha256(p.read_bytes()).hexdigest()}
assert not git("diff","--cached","--name-only"), "Index must remain unchanged"
changed=set(git("diff","--name-only").splitlines()+git("ls-files","--others","--exclude-standard").splitlines())
changed.discard("")
evidence_prefix=here.relative_to(root).as_posix()+"/"
sources=[root/p for p in sorted(changed) if not p.startswith(evidence_prefix) and (root/p).is_file()]
binary_root=root/"tools/NovaCore.App/bin/Release/net10.0-windows"
binaries=sorted(binary_root.glob("NovaCore*.dll"))+[binary_root/"NovaCore.exe"]
artifacts=sorted(p for p in here.rglob("*") if p.is_file() and p.name!="seal.json" and "__pycache__" not in p.parts)
result={"schema":1,"sealedUtc":datetime.now(timezone.utc).isoformat(),"head":git("rev-parse","HEAD"),
    "branch":git("branch","--show-current"),"indexUnchanged":True,"banked":False,
    "scope":"Current final working files/binaries plus retained evidence. Capture provenance and subsequent narrow corrections are documented in memory-receipt.json.",
    "sources":[item(p) for p in sources],"binaries":[item(p) for p in binaries],"evidence":[item(p) for p in artifacts]}
(here/"seal.json").write_text(json.dumps(result,indent=2)+"\n",encoding="utf-8")
for group in ("sources","binaries","evidence"):
    for entry in result[group]:
        assert hashlib.sha256((root/entry["path"]).read_bytes()).hexdigest()==entry["sha256"]
print(f"Sealed and verified {len(sources)} source files, {len(binaries)} binaries, {len(artifacts)} evidence files; index unchanged.")
