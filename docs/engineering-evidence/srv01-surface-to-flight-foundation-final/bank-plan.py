"""Create an exact proposed path allowlist. Never stages the real index or commits."""
from pathlib import Path
import json,hashlib,subprocess
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
def git(*a):return subprocess.check_output(['git',*a],cwd=ROOT,text=True).strip()
def save(n,v):(OUT/n).write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
changed=set(git('diff','--name-only').splitlines())|set(git('ls-files','--others','--exclude-standard').splitlines())
sourcePrefixes=('src/','tests/','samples/','native/','tools/')
docs={'docs/NOVACORE_CURRENT_STATE.md','docs/CODEX_HANDOFF.md','docs/architecture.md','docs/engineering-evidence/README.md'}
evidencePrefixes=('docs/engineering-evidence/srv01-surface-to-flight-gauntlet/','docs/engineering-evidence/srv01-supported-contact-admission-design/','docs/engineering-evidence/post-m15.2-production-remeasurement/','docs/engineering-evidence/srv01-surface-to-flight-foundation-final/')
duplicates={x['dispose'] for x in json.loads((OUT/'duplicate-evidence-retirement.json').read_text())}
include=sorted(p for p in changed if (p.startswith(sourcePrefixes+evidencePrefixes) or p in docs) and p not in duplicates)
# Reserve the final self-describing reports before generating the exact manifest.
for name in ['bank-prep.json','bank-manifest.json','verification.json','verification.md','evidence-identity.json','cleanup.md']:
    p=(OUT/name).relative_to(ROOT).as_posix()
    if p not in include:include.append(p)
include.sort()
excluded=[dict(path=p,reason='Exact redundant historical inventory/ref snapshot; manual disposal pending.' if p in duplicates else 'Unrelated preexisting review/evidence material; not part of the Stage1–5 bank.') for p in sorted(changed-set(include))]
assert all('docs/legal/' not in p and '/build/' not in '/'+p and '/bin/' not in p and '/obj/' not in p for p in include)
counts={'productionAndTests':sum(p.startswith(sourcePrefixes) for p in include),'currentDocs':sum(p in docs for p in include),'engineeringEvidence':sum(p.startswith(evidencePrefixes) for p in include),'total':len(include)}
save('bank-manifest.json',include)
save('bank-prep.json',dict(status='PROPOSED ONLY; no staging/commit/tag/push authority',baseline='ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5',branch='codex/srv01-supported-contact-admission',milestoneNumber=None,title='Canonical SRV-01 Florida Supported-Flight Foundation',proposedCommitMessage='NovaCore: Bank canonical SRV-01 Florida supported-flight foundation (insert milestone only after Project Control assigns it)',proposedAnnotatedTagSuffix='canonical-srv01-florida-supported-flight-foundation',counts=counts,includeManifest='bank-manifest.json',includeManifestSha256=hashlib.sha256((OUT/'bank-manifest.json').read_bytes()).hexdigest(),excluded=excluded,sourceIdentity='identity.json',regression='regression.json',evidenceIdentity='evidence-identity.json',manual='PASS by Project Control; final simplified slab',residuals='known-residuals.md',reproduction='reproduce.md',sequence=['Obtain explicit Project Control milestone/bank authorization.','Recheck expected authoritative remote main and all67 historical tags; stop if moved.','Verify final702 source/input seals and evidence identity, inspect explicit include/exclude list.','Resolve reviewed manual cleanup or explicitly retain it; never retry rejected automation.','Review final docs and prepare an alternate index from main with exactly the manifest paths; require whitespace/tree/withheld review.','Only when authorized, stage exact paths, compare real index to reviewed manifest/tree.','Create one atomic milestone commit; annotated tag only with assigned identity; never move a historical tag.','Recheck remote, fast-forward push only if authorized, verify remote main/tag/tree/public claims and working-tree boundaries.']))
print(counts,'excluded',len(excluded))
