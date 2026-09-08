"""Seal compact evidence after validation, guarded cleanup and the final smoke."""
import gzip,hashlib,json,pathlib,re,subprocess
import archive
HERE,ROOT=archive.HERE,archive.ROOT
def read(name):
    p=HERE/name
    return json.loads(p.read_bytes() if p.exists() else gzip.decompress(p.with_suffix(p.suffix+'.gz').read_bytes()))
def write(name,data): (HERE/name).write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
validation=read('validation.json');latest={x['label']:x for x in validation}
assert all(x['exitCode']==0 and not x['validationErrors'] for x in latest.values())
assert [(x['label'],x['exitCode']) for x in validation if x['exitCode']]==[('Debug-window',1)]
assert all(read('candidate-'+pose+'-parity.json')['passResult'] for pose in ['florida','inland'])
close=read('closeout.json');assert close['postCleanupSmoke']=='PASS' and close['frames']==240
assert not archive.SCRATCH.exists() and not archive.FAILED.exists()
compressed=read('compressed-journals.json')
for row in compressed['files']:
    p=HERE/row['destination'];assert not (HERE/row['source']).exists()
    assert archive.sha(p)==row['compressedSha256']
    assert hashlib.sha256(gzip.decompress(p.read_bytes())).hexdigest()==row['sourceSha256']
summary=dict(final='PASS',initialFailure=dict(label='Debug-window',result='3 pass / 1 fail / 0 skip',cause='Live probe repeated CPU scalar reads from mapped local GPU working buffers; unchanged 600-second timeout',resolution='Opt-in after-fence CPU snapshot, equations/assertions/timeout unchanged; complete Debug and Release windows pass, final guarded-source focused tests pass'),managed={c:dict(headless=dict(passCount=79,fail=0,skip=0),gpu=dict(passCount=8,fail=0,skip=0),window=dict(passCount=4,fail=0,skip=0)) for c in ['Debug','Release']},native={c:dict(gpuExecutables=2,facilityCases=102,materialCoordinateComponents=540,regionalCpu='PASS',mappedMemoryChecks=14) for c in ['Debug','Release']},launcherPass=15,routesPass=6,floridaRouteSmokeFrames=500,fullTraversalFrames=1701,levels='L0-L17',warpFrames=779,postCleanupFrames=240,strictVulkan='PASS; no final validation errors or VUID suppression',latest={n:dict(exitCode=x['exitCode'],wallSeconds=x.get('wallSeconds'),validationErrors=x['validationErrors']) for n,x in latest.items()},productionSourceHashes=close['sourceHashes'])
write('validation-summary.json',summary)

p=HERE/'README.md';s=p.read_text(encoding='utf-8')
s=re.sub(r'\*\*Closeout remains PENDING.*?\n', '**Closeout PASS.** The final unbanked candidate passed the gates below, including the sole bounded diagnostic revision, exact-output comparisons, final route/traversal checks and the post-cleanup deployed Florida smoke. Project Control owns acceptance and banking.\n',s,count=1)
s=s.replace('meaningful measured winner, full qualification pending','meaningful measured winner; qualification PASS')
s=s.replace('Full quality/validation qualification remains pending.','Full quality/validation qualification passed as recorded below.')
s=s.replace('both normal configurations are rebuilt and its focused live Debug/Release reruns remain pending.','both normal configurations are rebuilt and its focused live Debug/Release reruns passed in 128.759 / 113.893 seconds.')
s=s.replace('not the pending final post-revision deployment seal','distinct from the final post-revision deployment seal')
s=s.replace('Required route/dynamic, ownership/lifetime and final focused regression checks remain promotion gates.','Required route/dynamic, ownership/lifetime and final focused regression checks also passed; these provide their own contract coverage, not a claim of full per-frame raster attachment capture.')
start=s.index('## Validation')
s=s[:start]+'''## Validation

| Gate | Debug | Release |
|---|---|---|
| Managed headless | 79 pass / 0 fail / 0 skip | 79 pass / 0 fail / 0 skip |
| Managed GPU, no window | 8 pass / 0 fail / 0 skip | 8 pass / 0 fail / 0 skip |
| Managed visible window | 4 pass / 0 fail / 0 skip | 4 pass / 0 fail / 0 skip |
| Native GPU executables | 2 pass | 2 pass |
| Native regional CPU contract | PASS | PASS |
| Mapped-memory policy | 14 checks PASS | 14 checks PASS |
| Native and managed builds | PASS | PASS |
| Final guarded-source live probe | PASS, 128.759 s | PASS, 113.893 s |

Each native GPU suite covers 102 facility-visibility cases and 540 material-coordinate components. The original Debug window failure is retained, not silently overwritten: three tests passed and the live probe hit its unchanged 600-second timeout. The snapshot revision resolved it; the complete category then passed in 158.034 s (Release 118.931 s). The final empty-index guard was rebuilt and rechecked through the same live case in both configurations. Unrelated earlier headless/GPU results are not presented as new runs after a probe-only edit. Category exclusions are test-selection boundaries; selected tests have no skips.

The 15 launcher regressions and all six supported route probes passed. The launcher-selected Florida route completed its 500-frame smoke with stable seating/ready sole ownership. Full L0-L17 traversal completed 1,701 frames, ten snaps, three reversals and 58 publications, with zero missing/overlapping/stale owners and zero measured physical-height/normal parity error. Warp completed 779 frames with unchanged pupil/topology publication and zero owner failures. The final post-cleanup normal Florida smoke completed 240 frames; no private host or capture environment was used.

Florida/inland complete prepared64-byte records, oriented triangle multisets, D32, HDR and final image are exact against pre-change captures at identical logged inputs. Strict Vulkan remained enabled; no VUID filter or production validation suppression was added. Shaders: all49 per configuration remain byte-identical to baseline. All three required terrain asset hashes remain identical; both production cache packages verify. Normal Debug/Release dependencies resolve to their own builds.

Final normal Release native SHA-256: `33185eea351c7c8148ef0348725aa10bb3953b23643e384e18f789ca9796f3a5`. Debug: `e7857516ca9223966d1f43ee02d6c360576c5f6fd6452e283c7e91eb682f59a9`. Full managed/app/shader/asset identities are in [candidate-deployment-final.json](candidate-deployment-final.json) and [closeout.json](closeout.json). These are separate from the instrumented private measurement identity. [validation-summary.json](validation-summary.json) maps the final gate results; the full validation journal preserves the initial failure and reruns.

Normal manual entry remains `E:/NovaCore/tools/NovaCore.Launcher/bin/Release/net10.0-windows/NovaCore.Launcher.exe`, preset **Florida Launch Site**, resolving to `--scene=sol --focus=earth --surface-site=florida-launch --physical-surface=m12d-natural-candidate`. No launcher, shader or asset change was required.

## Lead judgment and strategic classification

**PASS**

**M13.5 CANDIDATE — READY FOR PROJECT CONTROL ACCEPTANCE**

**KEEP M13 OPEN FOR ONE PROVEN RESPONSIBILITY**

Proposed title: **NovaCore M13.5: Prefer local GPU memory for terrain working data**.

One responsibility passed the meaningful-payoff gate; the candidate is implemented and remains unbanked. VERIFY A found no remaining correctness blocker; VERIFY B found meaningful net benefit with the documented run-order, input-join, CPU and residual-frame limitations. This decision does not claim that M13's 8.33 ms objective is universally met, or authorize another mixed speculative optimizer. Project Control decides candidate acceptance and the subsequent M13 closure decision. No main merge, stage, commit, push, tag or M14 work occurred.

## Evidence lifecycle and storage

Permanent retention is bounded to this report, whole-frame cost/ranking journals, runtime/input/asset/shader hashes, exact-output summaries, narrow KSA/ISA provenance, verification and reproduction source. The initial no-raw plan was narrowed during the cheap correctness gate to one reusable capture slot, bounded to 512 MiB; no bulk sequence or video was retained. All three raw files are now removed. Captures are correctness observations and never counted as quiet performance runs.

The final guarded cleanup removed **152 classified Git-ignored scratch files**: **200,307,167 logical bytes by name**, of which **133,198,303 logical / 133,438,560 allocated bytes** were exclusive data. The 67,108,864-byte elevation name was only a scratch hard link; its production name and hash remain intact and none of those asset bytes are claimed as recovered. This includes the failed Debug probe's generated observations after the cause and successful unchanged-test reruns were recorded. The normal deployment, assets, caches, source, fixtures and five earlier untracked evidence packages are preserved.

The33 large journals are losslessly archived, from17,681,771 plain bytes to1,482,139 gzip bytes. [compressed-journals.json](compressed-journals.json) records original and compressed hashes; every decompressed stream is checked against its original SHA-256. Reports and small summaries remain directly readable. Original journal names in logs are provenance references. To inspect a journal, decompress its `.json.gz` to temporary storage; for full reproduction, expand needed journals into an isolated copy before adapting retained scripts. Do not run archived mutation/cleanup scripts against an unrelated current checkout.

Created means the bounded final materialized evidence/scratch inventory, not cumulative capture write I/O or build/test outputs already self-cleaned. The same three raw paths were reused. [seal.json](seal.json) records exact final created/retained/disposed accounting and the retained package size against the12 MiB budget. [disposable-manifest.json](disposable-manifest.json) lists every retired scratch name and hash. Disposable remaining: **zero**. The post-cleanup smoke's tiny process-local layer manifest was also removed. Normal rebuild outputs intentionally remain deployed and are not diagnostic debris.

## Remaining limits

Regional transitions still miss8.33 ms;61 of the64 matched actual-candidate replacement frames exceed11.11 ms. Fixed Florida remains8.87728-9.19036 ms across actual-candidate quiet runs, and application CPU pacing is slower than GPU timing. One normal baseline CPU validation/upload outlier remains causally mixed, with no guarantee of permanent elimination. Two opt-in one-shot lineage/projection traces retain potentially slow scalar mapped reads; they are not recurring production work. Performance benefit on other GPUs or when the original host fallback is required is unproven. None of these limits is hidden by reducing tessellation, material contribution or physical/publication guarantees.

## Final Git status

`git diff --check`: PASS (line-ending conversion warnings only). Staged diff: empty. HEAD/main/origin/main/M13.4 remain047ae479; M13.3 is unchanged. Existing earlier evidence stays untracked and untouched.

```text
'''+close['gitStatusShort'].rstrip()+'''
```
'''
for row in compressed['files']:
    s=s.replace(']('+row['source']+')',']('+row['destination']+')')
for old,new in {
    'records the starting six untracked evidence directories':'records six untracked directories at baseline capture: five earlier packages plus this newly created evidence directory',
    'all49':'all 49','prepared64-byte':'prepared 64-byte','The33':'The 33',
    'from17,681,771':'from 17,681,771','to1,482,139':'to 1,482,139',
    'the12 MiB':'the 12 MiB','miss8.33':'miss 8.33','ms;61':'ms; 61',
    'the64 matched':'the 64 matched','exceed11.11':'exceed 11.11',
    'remains8.87728':'remains 8.87728','remain047ae479':'remain 047ae479',
}.items():s=s.replace(old,new)
p.write_text(s,encoding='utf-8')

p=HERE/'verify-a.md';s=p.read_text(encoding='utf-8')
if not s.startswith('> Lead closeout addendum:'):s='''> Lead closeout addendum: final guarded-source live tests passed in Debug and Release (128.759 / 113.893 seconds); six launcher routes, 500-frame Florida smoke, 1,701-frame L0-L17 traversal, 779-frame warp and 240-frame post-cleanup Florida smoke all passed. The pending gates named in the review checkpoint below are now resolved. No correctness blocker remains. See validation-summary.json and closeout.json; the review chronology and limits below are preserved.

'''+s
for row in compressed['files']:s=s.replace(']('+row['source']+')',']('+row['destination']+')')
p.write_text(s,encoding='utf-8')

# Journal hashes above describe logical pre-compression bytes. The file manifest
# seals current retained bytes; it excludes itself and the self-sized seal only.
files=[]
for p in sorted(HERE.rglob('*')):
    if not p.is_file() or p.name in ['retained-manifest.json','seal.json']:continue
    if p.suffix=='.json':json.loads(p.read_bytes())
    if p.suffix=='.gz':json.loads(gzip.decompress(p.read_bytes()))
    files.append(archive.record(p,HERE))
write('retained-manifest.json',dict(files=files,excludedSelfReferential=['retained-manifest.json','seal.json'],reason='Compact causal model, exact runtime/input/output provenance, original failing result and verified remedy, regressions, reproduction and narrow KSA evidence'))
disposed=read('disposable-manifest.json')
seal=dict(status='PASS',budgetBytes=12*1024*1024,scope='Final materialized ticket evidence and scratch only; not cumulative capture I/O, entire workspace or ordinary self-cleaned test/build output',scratchFilesDisposed=disposed['count'],scratchLogicalBytesDisposedByName=disposed['logicalBytesByName'],scratchExclusiveLogicalBytesDisposed=disposed['exclusiveLogicalBytes'],scratchExclusiveAllocatedBytesDisposed=disposed['exclusiveAllocatedBytes'],productionHardlinkDataRetainedBytes=disposed['protectedHardlink']['bytes'],journalsArchived=len(compressed['files']),originalJournalBytes=sum(r['sourceBytes'] for r in compressed['files']),compressedJournalBytes=sum(r['compressedBytes'] for r in compressed['files']),disposableRemainingFiles=0,disposableRemainingBytes=0,postCleanupSmoke='PASS',gitDiffCheck='PASS',retainedBytes=0,retainedFiles=0,createdMaterializedLogicalBytes=0,disposedExclusiveLogicalBytes=0)
for _ in range(12):
    write('seal.json',seal)
    allfiles=[p for p in HERE.rglob('*') if p.is_file()]
    retained=sum(p.stat().st_size for p in allfiles)
    retired=seal['scratchExclusiveLogicalBytesDisposed']+seal['originalJournalBytes']-seal['compressedJournalBytes']
    if seal['retainedBytes']==retained and seal['retainedFiles']==len(allfiles):break
    seal.update(retainedBytes=retained,retainedFiles=len(allfiles),createdMaterializedLogicalBytes=retained+retired,disposedExclusiveLogicalBytes=retired)
else:raise AssertionError('seal size did not stabilize')
assert retained<=seal['budgetBytes']
assert all(archive.sha(HERE/f['relative'])==f['sha256'] for f in files)
check=subprocess.run(['git','diff','--check'],cwd=ROOT,capture_output=True,text=True);assert check.returncode==0
assert subprocess.check_output(['git','status','--short'],cwd=ROOT,text=True)==close['gitStatusShort']
print(json.dumps(seal,indent=2))
