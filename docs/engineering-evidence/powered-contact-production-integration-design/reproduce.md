# Reproduce this architecture gate without a campaign

1. Read the consolidated `PRODUCTION-DIRECTION-HANDOFF.md`, `CONSOLIDATION-MANIFEST.md`, `CANDIDATE-A-HISTORY.md`, `REPRODUCTION-INDEX.md`, and `final-inventory.json` under `E:\NovaCore\docs\engineering-evidence\powered-contact-ordinary-step-event-closure`.
2. Use the targeted current source anchors in `novacore-current-owner-map.md` and `mass-stop.md`. The latter narrows the only historical detail needed: ordinary closure README mass/admission limits. Do not replay the numerical research forest.
3. Recheck actual KSA install hashes/version; tokens are valid only for the recorded DLL. Existing member decoder can print one method to stdout:

```powershell
dotnet 'C:\Users\Tyler\AppData\Local\Temp\ilspycmd-11.0.0.9375\tools\net10.0\any\ilspycmd.dll' `
  --disable-updatecheck -m 0x06001C19 'E:\Kitten Space Agency\KSA.dll'
```

Compare `06001B92` (unconstrained integration), `06001B40` (native mass upload), `06001B46` (consume) and `06001B43` (recompute). The source review table provides the other exact members. Do not use `-il -m`: this tool version can ignore member scoping in that combination. No local source-copy tree or build is needed. If existing tool is missing, report that rather than installing or generating a bulk dump as part of this gate.

4. Open the three actual Discord message links in `ksa-current-review.md` in the authorized authenticated channel and read the entry. History is navigation/provenance, not current implementation. If unavailable on a future run, report it and do not infer completion.
5. Read the subsequently authorized bounded investigation in mass-stop.md: exact contact outcomes, analytical mass-only formulas, ten retained native results and later friction-work results. It resolves the policy as A for that declared scope; no broad mass scheme or M15.0 tolerance substitution. No new solver/performance run is required to repeat the source/retained-evidence reasoning.

Mass arithmetic can be repeated without creating files (PowerShell7, existing Python):

```powershell
@'
import json
from fractions import Fraction as F
from pathlib import Path
p=Path('E:/NovaCore/docs/engineering-evidence/powered-contact-ordinary-step-event-closure/inputs.json')
out=[]
for row in json.loads(p.read_text())['cases']:
    x=row['input']; h=F(x['h']); q=F(x['q']); H=F(x['H']); m=F(x['m0'])
    k=F(x['k']); c=F(x['c']); A=F(x['fx'])+k*F(x['fy'])
    j=F(981,100)*q*h*(H-h/2)
    v=abs(A)*q*h*h/(2*m*8); w=c*j/2
    out.append((x['name'],q*h,q*h/8,j,k*j,c*j,k*j/2,v,w,H*v,H*w))
labels=['massLoss','relativeChange','normalImpulse','tangentImpulse','twistImpulse','normalMoment','tangentVelocity','yawRate','positionMassOnly','orientationMassOnly']
for n,label in enumerate(labels,1):
    row=max(out,key=lambda r:r[n]); print(label,row[0],format(float(row[n]),'.15g'))
'@ | python -B -
```

These are ideal mass-only effects, not a native error subtraction or compliance bound. The actual already-retained native output comparisons are decisive for the bounded physical gates. The reproduction index's F entries can separately recheck saved friction traces if needed; no trace recapture is required here.

## Non-destructive final verification

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
git branch --show-current
git rev-parse HEAD main origin/main
git ls-remote origin refs/heads/main
git status --short
git diff --name-only
git diff --cached --name-only
git diff --check
git diff --cached --check

$gateIdentity = Get-Content -LiteralPath 'E:\NovaCore\docs\engineering-evidence\powered-contact-production-integration-design\identity.json' -Raw | ConvertFrom-Json
$gateIdentity.inputs | ForEach-Object {
    [pscustomobject]@{ Path = $_.path; Match = ((Get-FileHash -LiteralPath $_.path -Algorithm SHA256).Hash -eq $_.sha256) }
}
$gateIdentity.ksa | ForEach-Object {
    [pscustomobject]@{ Path = $_.path; Match = ((Get-FileHash -LiteralPath $_.path -Algorithm SHA256).Hash -eq $_.sha256) }
}
Compare-Object -ReferenceObject $gateIdentity.tags -DifferenceObject @(git for-each-ref --format='%(refname) %(objectname) %(*objectname)' refs/tags)
```

An empty tag comparison and all `Match=True` prove the recorded targeted identities, not a whole-repository historical hash campaign. Git checks cover tracked production/native/tests. Blender was not accessed or changed by this work.

## Cleanup

This ticket created **no disposable build, bin, obj, scratch, generated diagnostic source, or temporary output**: 0 paths, 0 files, 0 bytes. Inspection used retained tools with stdout. The new Markdown/JSON files are concise retained evidence, not disposable scratch. Therefore no removal command is appropriate and no deletion was attempted. Existing consolidated evidence/reproduction sources remain retained. A future ticket that creates output must inventory exact paths/counts/bytes and print its reviewed manual cleanup command separately.
