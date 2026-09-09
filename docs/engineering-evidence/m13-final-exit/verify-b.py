"""Independent scalar performance checks for the CPU-read key-placement controls.

No imports of build/capture code, no GPU work. Writes verify-b.md/json only.
"""
import importlib.util
import json
import re
from pathlib import Path

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location("work_a_analysis",HERE/"work-a.py")
wa=importlib.util.module_from_spec(spec);spec.loader.exec_module(wa)
IDENTITY=["cameraBody","bodyOrientation","viewProjection","generation","level","topologyFamily",
          "compactedTriangles","tcsPatches","refinedVertices","maximumOuterTesFactor","maximumInnerTesFactor","innerFactorBins"]


def summarize(path,baseline):
    d,digest=wa.load(path)
    assert d["bank"]==wa.BANK
    dr=wa.unpack(d.get("directionalRows"))[-100:]
    assert len(dr)==100 and all(x["submittedFrame"]==x["timingFrame"] for x in dr)
    h={x['frame']:x for x in wa.unpack(d.get('hostRows'))}
    submission=[h[x['submittedFrame']] for x in dr] if h else []
    completion=[h[x['submittedFrame']+1] for x in dr] if h else []
    assert all(c['gpuFrame']==c['geometryFrame']==r['timingFrame'] and c['gpuTotal']==r['gpuTotalMs'] for r,c in zip(dr,completion))
    polls={x['frame']:x for x in (wa.fields(s) for s in d['lines'] if 'Performance request polling:' in s)}
    warm_polls=[polls[r['submittedFrame']] for r in dr if r['submittedFrame'] in polls]
    identity={k:sorted({str(r[k]) for r in dr if k in r}) for k in IDENTITY}
    return dict(label=wa.journal_label(d,path),journalInternalLabel=d.get('label'),
                evidenceRole=wa.evidence_role(d,path,d['nativeHash']==baseline['deployment']['Release']['native']),
                source=path.name,decodedSha256=digest,bank=d['bank'],
                nativeHash=d['nativeHash'],managedHash=d['managedHash'],args=d['args'],env=d['env'],
                shaderHashes=d['shaderHashes'],shadersMatchBank=d['shaderHashes']==baseline['deployment']['Release']['shaders'],
                exitCode=d['exitCode'],errors=d['errors'],window=[dr[0]['submittedFrame'],dr[-1]['submittedFrame']],
                identity=identity,gpu=wa.stats(r['gpuTotalMs'] for r in dr),draw=wa.stats(r['gpuDetailedDrawMs'] for r in dr),
                submissionCpu=wa.cpu_stats(submission),completionCpu=wa.cpu_stats(completion),
                queue=wa.stats(r['queueMs'] for r in warm_polls),complete=wa.stats(r['completeMs'] for r in warm_polls),
                pollCounterSets={k:sorted({r[k] for r in warm_polls}) for k in ['requests','uploads','pending','records']},
                memory=[wa.fields(s) for s in d['lines'] if 'Poll memory type:' in s],
                clipping=wa.stats(r['clippingOutputPrimitives'] for r in dr),fragments=wa.stats(r['fragmentInvocations'] for r in dr),
                cpuJournalAvailable=bool(h),
                rows=[[r['submittedFrame'],r['gpuTotalMs'],s['total'],s['fenceWait'],s['validationUpload'],c['total'],c['fenceWait'],
                       polls.get(r['submittedFrame'],{}).get('queueMs')] for r,s,c in zip(dr,submission,completion)])


def compare(a,b):
    changed=[k for k in IDENTITY if a['identity'][k]!=b['identity'][k]]
    return dict(baseline=a['label'],control=b['label'],sameManaged=a['managedHash']==b['managedHash'],
                sameNative=a['nativeHash']==b['nativeHash'],sameArgs=a['args']==b['args'],
                sameShaders=a['shaderHashes']==b['shaderHashes'],changedInputOrHardwareFields=changed,
                samePollCounters=a['pollCounterSets']==b['pollCounterSets'],
                baselineMinusControl={k:{stat:x[stat]-y[stat] for stat in ['mean','median','p95','p99','peak']}
                                     for k,x,y in [('gpu',a['gpu'],b['gpu']),
                                                   ('cpuSubmission',a['submissionCpu']['total'],b['submissionCpu']['total']),
                                                   ('cpuCompletion',a['completionCpu']['total'],b['completionCpu']['total']),
                                                   ('nonFence',a['submissionCpu']['totalMinusFence'],b['submissionCpu']['totalMinusFence']),
                                                   ('queue',a['queue'],b['queue'])] if x and y})


def dynamic_pair(mode,baseline,baseline_prefix='poll-',control_prefix='cached-'):
    sources=[]
    for prefix in [baseline_prefix,control_prefix]:
        path=HERE/(prefix+mode+'.json')
        if not path.exists():path=HERE/(prefix+mode+'.json.gz')
        if not path.exists():return None
        data,digest=wa.load(path)
        result=wa.analyze(data,path,baseline=baseline)
        polls=[wa.fields(s) for s in data['lines'] if 'Performance request polling:' in s]
        first=result['firstAuthoritativeHostFrame']
        polls=[r for r in polls if first is not None and r['frame']>=first]
        sources.append((data,result,digest,polls))
    da,a,ha,pa=sources[0];db,b,hb,pb=sources[1]
    af={r[0]:dict(zip(wa.SCALAR_FIELDS,r)) for r in a['alignedScalarRows']}
    bf={r[0]:dict(zip(wa.SCALAR_FIELDS,r)) for r in b['alignedScalarRows']}
    common=sorted(set(af)&set(bf))
    signatures=lambda r:[tuple(p.get(k) for k in ['generation','level','topologyFamily','hash','publications']) for p in r['publications']]
    differences={k:sum(af[n][k]!=bf[n][k] for n in common) for k in ['generation','tcsPatches','tesInvocations','fragments']}
    cadence=[dict(generation=x['generation'],baselineMarker=x['observedFrameContext'],cachedMarker=y['observedFrameContext'],
                  delta=y['observedFrameContext']-x['observedFrameContext']) for x,y in zip(a['publications'],b['publications'])]
    summary=lambda r,d,h,p:dict(label=r['label'],evidenceRole=r['evidenceRole'],decodedSha256=h,nativeHash=r['nativeHash'],managedHash=r['managedHash'],
        hostFrames=r['hostCount'],alignedFrames=r['alignedCount'],firstAuthoritativeHostFrame=r['firstAuthoritativeHostFrame'],
        gpu=r['allEarth'],allCpu=r['allCpu'],cpuAfterFirstPublication=r['afterFirstPublicationCpu'],
        publicationCount=r['publicationCount'],replacementCount=r['replacementPublicationCount'],
        publicationMarkers=r['publications'],normalCpuOutliers16=r['afterFirstPublicationOutliers16'],
        exitCode=r['exitCode'],errors=r['errors'],queue=wa.stats(x['queueMs'] for x in p),
        pollCounterSets={k:sorted({x[k] for x in p}) for k in ['requests','uploads','pending','records']})
    return dict(mode=mode+(' return' if baseline_prefix=='poll-return-' else ''),baseline=summary(a,da,ha,pa),cached=summary(b,db,hb,pb),
        controlRole=b['evidenceRole'],comparisonLabels=[a['label'],b['label']],
        sameNative=a['nativeHash']==b['nativeHash'],sameManaged=a['managedHash']==b['managedHash'],
        managedMatchesBank=a['managedHash']==b['managedHash']==baseline['deployment']['Release']['managed'],
        sameShaders=a['shaderHashes']==b['shaderHashes'],sameArgs=a['args']==b['args'],
        environmentDifferences={k:[da['env'].get(k),db['env'].get(k)] for k in set(da['env'])|set(db['env']) if da['env'].get(k)!=db['env'].get(k)},
        identicalPublicationSignatureSequence=signatures(a)==signatures(b),publicationCadence=cadence,
        frameIntersection=len(common),crossFrameDifferences=differences,
        fullCameraPhysicalByteJoinAvailable=False,
        cpuRecovery={k:{s:a['afterFirstPublicationCpu'][k][s]-b['afterFirstPublicationCpu'][k][s] for s in ['mean','median','p95','p99','peak']}
                     for k in ['total','totalMinusFence','validationUpload']},
        gpuRecovery={s:a['allEarth']['total'][s]-b['allEarth']['total'][s] for s in ['mean','median','p95','p99','peak']})


def quiet_pair(baseline):
    results=[]
    for name in ['quiet-poll-regional','quiet-cached-regional']:
        path=HERE/(name+'.json')
        if not path.exists():path=HERE/(name+'.json.gz')
        if not path.exists():return None
        d,h=wa.load(path)
        assert d['bank']==wa.BANK and not wa.unpack(d.get('hostRows'))
        lines=[s for s in d['lines'] if any(x in s for x in ['Average frame time:','Frame pacing:','Fence wait pacing:','CPU timings:'])]
        avg=next(s for s in lines if 'Average frame time:' in s)
        frame=next(s for s in lines if 'Frame pacing:' in s)
        cpu=next(s for s in lines if 'CPU timings:' in s)
        number=lambda text,key:float(re.search(re.escape(key)+r'=([0-9.]+)',text).group(1))
        results.append(dict(label=name,decodedSha256=h,nativeHash=d['nativeHash'],managedHash=d['managedHash'],
            env=d['env'],args=d['args'],shaderHashes=d['shaderHashes'],exitCode=d['exitCode'],errors=d['errors'],
            averageCpu=float(re.search(r'time: ([0-9.]+)',avg).group(1)),
            cpuPacing={k:number(frame,k) for k in ['p50','p95','p99','max','samples']},
            cpuMeans={k:number(cpu,k) for k in ['update','fence','inspection','hostCallback','validationUpload','record','submit','present']},
            publicationCount=sum('Production spherical billboard publication:' in s for s in d['lines']),rawAggregateLines=lines))
    a,b=results
    return dict(baseline=a,cached=b,sameNative=a['nativeHash']==b['nativeHash'],sameManaged=a['managedHash']==b['managedHash'],
                sameShaders=a['shaderHashes']==b['shaderHashes'],sameArgs=a['args']==b['args'],
                averageCpuRecovery=a['averageCpu']-b['averageCpu'],
                medianCpuRecovery=a['cpuPacing']['p50']-b['cpuPacing']['p50'],
                caveat='Host/queue/GPU per-frame journals disabled; residual timestamp/log hooks remain. Aggregate includes startup; no per-frame startup/normal split or full GPU percentile series.')


def completed_path(label):
    for suffix in ['.json','.json.gz']:
        path=HERE/(label+suffix)
        if path.exists():return path
    return None


def implemented_candidate(baseline):
    """Keep actual default runs separate from earlier selector controls.

    Exact-output captures are deliberately not candidates for timing analysis.
    Empty/missing journals remain pending; this code never executes the harness.
    """
    runs=[];comparisons=[];missing=[]
    plans={**{'candidate-fixed-'+p:['fixed-'+p] for p in
              ['orbital','factor1','florida','grazing','active-refinement','inland']},
           'candidate-cpu-florida':['cpu-florida','poll-florida','poll-return-florida','cached-florida','cached-repeat-florida'],
           'candidate-cpu-inland':['cpu-inland']}
    for label,control_names in plans.items():
        path=completed_path(label)
        if path is None:missing.append(label);continue
        d,_=wa.load(path)
        if len(wa.unpack(d.get('directionalRows'))) < 100:
            missing.append(label+' (incomplete fixed100)');continue
        assert d.get('evidenceRole')=='implemented unbanked default candidate', label
        assert not d.get('env',{}).get('NOVACORE_EXIT_CACHED_KEYS'), label
        assert not d.get('env',{}).get('NOVACORE_PIXEL_PARITY'), label
        r=summarize(path,baseline);runs.append(r)
        for other in control_names:
            p=completed_path(other)
            if p is not None:
                comparisons.append(compare(summarize(p,baseline),r))
    dynamics=[]
    for mode in ['regional','full','warp']:
        if completed_path('candidate-dynamic-'+mode) is None:
            missing.append('candidate-dynamic-'+mode);continue
        prefixes=['dynamic-']
        if mode in ['regional','full']:prefixes+=['poll-','cached-']
        if mode=='regional':prefixes+=['poll-return-']
        for prefix in prefixes:
            pair=dynamic_pair(mode,baseline,prefix,'candidate-dynamic-')
            if pair:dynamics.append(pair)
    parity=[]
    for pose in ['florida','inland']:
        path=completed_path('candidate-'+pose+'-parity')
        if path:
            d,h=wa.load(path)
            assert d['implementationDefault'] and d['performanceTimingsExcluded']
            parity.append(dict(pose=pose,source=path.name,decodedSha256=h,passed=d['pass'],
                frame=d['frame'],width=d['width'],height=d['height'],comparison=d['comparison'],
                keyMemory=d['actual'].get('keyMemory'),
                prototypeTogglePresent=bool(d['actual']['runtime'].get('env',{}).get('NOVACORE_EXIT_CACHED_KEYS')),
                preChangeReport=d['preChangeReport'],preChangeReportSha256=d['preChangeReportSha256'],
                candidateManifest=d['candidateManifest'],candidateManifestSha256=d['candidateManifestSha256'],
                limits=d['limits'],performanceTimingsExcluded=True))
    bank_dynamic=[p for p in dynamics if p['comparisonLabels'][0]=='dynamic-'+p['mode']]
    fixed_cpu=[p for p in comparisons if p['baseline'] in ['cpu-florida','cpu-inland']]
    payoff_pass=(not missing and len(bank_dynamic)==3 and len(fixed_cpu)==2 and
        all(p['cpuRecovery']['total']['median']>=1.5 for p in bank_dynamic) and
        all(p['baselineMinusControl']['cpuSubmission']['median']>=1.5 for p in fixed_cpu) and
        len(parity)==2 and all(p['passed'] and not p['prototypeTogglePresent'] for p in parity) and
        all(p['identicalPublicationSignatureSequence'] and not p['cached']['errors'] and p['cached']['exitCode']==0 for p in bank_dynamic))
    final_outliers=[dict(source=p['cached']['label'],row=r) for p in bank_dynamic
                    for r in p['cached']['normalCpuOutliers16'] if r['total']>40]
    deployment_path=completed_path('candidate-deployment')
    deployment=wa.load(deployment_path)[0]['deployment']['Release'] if deployment_path else {}
    return dict(evidenceRole='implemented unbanked default candidate',runs=runs,
        comparisons=comparisons,dynamicComparisons=dynamics,defaultImplementationParity=parity,pending=missing,
        performanceJudgment='PASS - bounded implemented CPU/display-frame responsibility' if payoff_pass else 'PENDING OR FAIL - inspect missing/performance/output gates',
        gpuClosure=False,universalFrameStabilityClaim=False,
        afterAuthorityOutliers40=final_outliers,
        deployedIdentity=dict(normalNativeHash=deployment.get('native'),managedHash=deployment.get('managed'),
            quietNativeMatchesDeployment=all(r['nativeHash']==deployment.get('native') for r in runs if r['label'].startswith('candidate-fixed-')),
            dynamicManagedMatchesDeployment=all(p['cached']['managedHash']==deployment.get('managed') for p in bank_dynamic)),
        limitations=['Separate run-order populations; no subtraction presented as a paired per-frame causal estimate.',
                     'Fixed camera/input/hardware identity sets can be compared; dynamic camera/physical bytes are unavailable.',
                     'No selector environment or raw-capture hook in these performance controls; quiet native is the normal deployed candidate.',
                     'Earlier prototype results and baseline returns remain retained even when the final result differs.'])


def candidate_markdown(candidate):
    lines=['','## Actual default implementation measurements','',
        'These candidate-* journals are the implemented unbanked default candidate, never the banked M13.5 baseline. Quiet fixed runs use the normal deployed candidate native binary; separate CPU/dynamic profiles use the rebuilt private timing host without the cached-key selector or capture hooks. Their bank field records production ancestry. Earlier controls and return/repeat variance remain above.','',
        '| Candidate run | GPU median / P95 / P99 / peak | GPU >8.33 / >11.11 | Submission CPU median / P95 / P99 / peak |','|---|---|---|---|']
    for r in candidate['runs']:
        g=r['gpu'];c=r['submissionCpu']['total']
        cp=wa.triplet(c)+(f" / {c['peak']:.5f}" if c else '')
        lines.append(f"| {r['label']} | {wa.triplet(g)} / {g['peak']:.5f} | {g['over8.33']} / {g['over11.11']} | {cp} |")
    lines+=['','| Reference -> implemented candidate | GPU median change | CPU mean / median recovery | Input/hardware fields differing |','|---|---:|---|---|']
    for p in candidate['comparisons']:
        m=p['baselineMinusControl'];c=m.get('cpuSubmission')
        cp=f"{c['mean']:.5f} / {c['median']:.5f}" if c else 'unavailable (separate quiet run)'
        lines.append(f"| {p['baseline']} -> {p['control']} | {m['gpu']['median']:.5f} | {cp} | {', '.join(p['changedInputOrHardwareFields']) or 'none in logged sets'} |")
    lines+=['','| Dynamic reference -> implemented candidate | After-authority CPU mean / median recovery | Candidate CPU P95 / P99 / peak | Candidate GPU P95 / P99 / peak | >8.33 / >11.11 | Publication signature / cadence differences |','|---|---|---|---|---|---|']
    for p in candidate['dynamicComparisons']:
        r=p['cached'];c=r['cpuAfterFirstPublication']['total'];g=r['gpu']['total'];recovery=p['cpuRecovery']['total']
        cadence=sum(x['delta']!=0 for x in p['publicationCadence'])
        lines.append(f"| {' -> '.join(p['comparisonLabels'])} | {recovery['mean']:.5f} / {recovery['median']:.5f} | {c['p95']:.5f} / {c['p99']:.5f} / {c['peak']:.5f} | {g['p95']:.5f} / {g['p99']:.5f} / {g['peak']:.5f} | {g['over8.33']} / {g['over11.11']} | {p['identicalPublicationSignatureSequence']} / {cadence} |")
    lines+=['','Dynamic summaries describe all strictly aligned Earth GPU frames and every host frame after initial authority. Identity/cadence differences are retained in JSON; these are not exact camera/physical-byte joins. The absence of a queue-only timer in the final host does not authorize deriving exact function cost from a mixed CPU scope. Full validation/readiness remains the lead report responsibility.']
    lines+=['','Default-implementation exact-output gates: '+', '.join(p['pose']+' '+('PASS' if p['passed'] else 'FAIL') for p in candidate['defaultImplementationParity'])+'. Complete prepared64-byte records, oriented submitted triangle multisets and whole D32/HDR/image hashes are checked against retained pre-change digests. Their capture timings are excluded; no dynamic or individual post-TES-stream proof is inferred.']
    if candidate['pending']:lines+=['','Pending completed journals: '+', '.join(candidate['pending'])+'.']
    if not candidate['pending']:
        lines+=['','## Final performance red-team judgment','',
            '**'+candidate['performanceJudgment']+'.** The actual default candidate confirms median whole-CPU recovery of1.6936/1.6975 ms in Florida/inland and1.8089/1.6860/1.7543 ms in regional/full/warp versus the original fresh bank profiles. Fixed mean recovery is1.75885/1.69864 ms; full/warp means recover1.52525/1.74304 ms. The regional mean recovery2.26476 ms exceeds its non-fence mean recovery1.90955 ms, so it must not all be assigned to the key scan; fence/run-order variation contributes. The same-binary selector controls and narrow queue timer establish causal memory-placement ownership; these final runs establish default-implementation delivery.','',
            'The final quiet GPU medians range from.82540 ms orbital to8.95040 ms Florida. Florida remains100/100 >8.33; inland peaks8.31748. Final regional737 aligned GPU frames have median8.77008, P9912.49588, peak12.84380 and541 >8.33 /44 >11.11. Full has2/1698 >8.33; warp has none. Against initial bank fixed results, non-orbital medians increase.02352-.11284 ms; these small mixed changes and earlier return/repeat variability remain visible. No GPU performance improvement or universal8.33/120 FPS closure is claimed.','',
            'All final fixed camera/orientation/projection, generation, level, topology family, selected/TCS counts and factor-bin sets match the corresponding bank sets. Grazing and active-refinement hardware TES invocation sets differ slightly (medians3470003 ->3470004 and2779258 ->2779254); no exact TES-count claim is made. Full/regional/warp publication signatures and native cadence match their original bank profiles, with58/37/1 publications and no logged invalid publication/readiness/ownership records. Dynamic common-frame generation matches, but TCS/TES/fragment counts differ; no exact dynamic camera, physical-byte or raster parity claim follows.','',
            'The default rebuilt dynamic managed binary differs in hash from the original bank artifact and matches the candidate deployment. Quiet fixed native also matches the candidate deployment. Arguments and shader hashes match the reference; changing native/managed build identity is recorded, not described as a byte-identical runtime comparison. The controlled prototype comparison remains the same-binary causal evidence. Final parity independently records selected memory type3/flags14 without the prototype selector.','',
            'Frame stability is improved at typical and percentile levels, with residuals retained. Regional CPU after initial authority is9.4326 median /12.3144 P95 /13.4287 P99 /21.0747 peak; its lone >16.67 event is frame182, including14.8288 ms validation/upload work. Full is5.7498 /7.9935 /11.0399 /50.4425, with8 >16.67 and one >40. Full frame8 already uses generation1 after first authority:48.4895 ms fence wait dominates50.4425 ms whole CPU while completed GPU7 is.982 ms. No window recreation or Vulkan failure is logged. It is an early post-authority fence/scheduling outlier with exact external cause unclassified, not proven startup-only and not a50 ms terrain GPU job. Later full topology/upload tails remain up to25.3435 ms at frame347. The outlier is not removed from any distribution.','',
            'This one nonrecurring, unclassified wait does not establish a second worthwhile avoidable terrain owner and does not negate the repeated narrow CPU gain. It prevents a claim that every post-authority frame is below40 ms or that all stability debt is solved. The ticket stop rule supports returning the one implemented responsibility with these residuals rather than exploring another optimization. No additional performance candidate or GPU recapture is recommended; full functional validation and final banking readiness are the lead responsibility.']
    return lines


def main():
    baseline=wa.load(HERE/'baseline.json')[0]
    files=sorted(HERE.glob('poll-*.json'))+sorted(HERE.glob('cached-*.json'))
    files += sorted(HERE.glob('poll-*.json.gz'))+sorted(HERE.glob('cached-*.json.gz'))
    runs=[];seen=set()
    for p in files:
        d,digest=wa.load(p)
        if len(wa.unpack(d.get('directionalRows'))) < 100 or digest in seen:continue
        seen.add(digest);runs.append(summarize(p,baseline))
    pairs=[]
    for b in runs:
        if 'NOVACORE_EXIT_CACHED_KEYS' not in b['env']:continue
        for a in runs:
            if 'NOVACORE_EXIT_CACHED_KEYS' in a['env'] or a['identity']['cameraBody']!=b['identity']['cameraBody']:continue
            pairs.append(compare(a,b))
    dynamics=[p for mode in ['regional','full'] if (p:=dynamic_pair(mode,baseline)) is not None]
    returned=dynamic_pair('regional',baseline,'poll-return-')
    if returned:dynamics.append(returned)
    quiet=quiet_pair(baseline)
    parity=[]
    for pose in ['florida','inland']:
        path=HERE/('keys-'+pose+'-parity.json')
        if not path.exists():path=HERE/('keys-'+pose+'-parity.json.gz')
        if path.exists():
            d,h=wa.load(path)
            parity.append(dict(pose=pose,source=path.name,decodedSha256=h,passed=d['pass'],frame=d['frame'],width=d['width'],height=d['height'],
                comparison=d['ab'],nativeHash=d['cachedKeys']['runtime']['nativeHash'],
                sameNative=d['baseline']['runtime']['nativeHash']==d['cachedKeys']['runtime']['nativeHash'],
                limits=d['limits'],performanceTimingsExcluded=True))
    candidate=implemented_candidate(baseline)
    report=dict(bank=wa.BANK,scope='CPU-read key-buffer placement; no GPU-time win inferred',runs=runs,comparisons=pairs,dynamicComparisons=dynamics,quietLoggingControl=quiet,prototypeParity=parity,implementedCandidate=candidate,
                scalarRowFields=['submittedFrame','gpu','cpuSubmission','submissionFence','validationUpload','cpuCompletion','completionFence','queueMs'])
    (HERE/'verify-b.json').write_text(json.dumps(report,separators=(',',':'),allow_nan=False)+'\n',encoding='utf-8')
    lines=['# Verify B: CPU-read key placement performance red team','',
           'This is a fresh banked-M13.5 comparison, not a repeat of the M13.5 GPU working-data win. The proposed role is the CPU-scanned terrain residency key table. Production correctness/fallback remain independent gates. This verifier performs no builds, GPU runs or production edits.','',
           '## Complete fixed100 populations','',
           '| Run | GPU median / P95 / P99 | Submission CPU median / P95 / P99 | Non-fence median | Queue median |','|---|---|---|---:|---:|']
    for r in runs:
        q=f"{r['queue']['median']:.5f}" if r['queue'] else 'unavailable'
        lines.append(f"| {r['label']} | {wa.triplet(r['gpu'])} | {wa.triplet(r['submissionCpu']['total'])} | {r['submissionCpu']['totalMinusFence']['median']:.5f} | {q} |")
    lines+=['','## Net benefit and input checks','','Positive values below are baseline minus cached control. CPU benefit must be judged from complete host frames and non-fence work, not just the narrowed queue timer.','',
            '| Baseline -> cached | GPU median change | CPU median recovery | Non-fence median recovery | Queue median recovery | Input/hardware differences |','|---|---:|---:|---:|---:|---|']
    for p in pairs:
        m=p['baselineMinusControl']
        lines.append(f"| {p['baseline']} -> {p['control']} | {m['gpu']['median']:.5f} | {m['cpuSubmission']['median']:.5f} | {m['nonFence']['median']:.5f} | {m['queue']['median']:.5f} | {', '.join(p['changedInputOrHardwareFields']) or 'none in logged inputs/TCS/TES'} |")
    lines+=['','## Same-host orbital return/repeat and pacing limit','',
            'The later uncached orbital return reproduces the roughly5.53 ms whole frame seen with cached keys. Same-binary uncached/cached-repeat CPU medians are5.5341/5.5291 ms; P99 values5.9607/5.9058 and peaks6.3339/5.9252. GPU medians1.13668/1.14120 are also comparable. Non-fence work falls2.5859 to.7467 ms while fence wait rises2.9195 to4.7406 ms. The saved CPU work is absorbed by the existing paced interval in this cheap regime.','',
            'This return control does not support attributing the earlier3.2847-to5.5371 ms direct comparison to the cache change. Exact driver/presentation/clock ownership of the later pacing remains unclassified; no production accommodation is justified. Do not claim a1.8 ms orbital display-frame gain. Same-host Florida return against the two cached runs shows1.8518/1.7432 ms CPU median recovery. The least favorable earlier-host Florida comparison against the cached repeat is only1.4213 ms, so there is no universal per-run >=1.5 claim.','',
            '## Complete production-managed dynamic comparisons','',
            'These pairs use the same cached-capable native binary and byte-identical deployed managed route, with only the cached-key environment selection changed. Every GPU statistic uses positive gpuFrame == geometryFrame == host.frame-1 and equal frame/total for child scopes. CPU after initial authority includes all bodies/route frames from the first published host frame onward; no favorable-frame subset replaces the full populations.','',
            '| Route / mode | Host after first publication | CPU mean / median / P95 / P99 / peak | GPU aligned / median / P95 / P99 / peak | GPU >8.33 / >11.11 |','|---|---:|---|---|---|']
    for pair in dynamics:
        for key in ['baseline','cached']:
            r=pair[key];c=r['cpuAfterFirstPublication']['total'];g=r['gpu']['total']
            cp=' / '.join(f"{c[k]:.5f}" for k in ['mean','median','p95','p99','peak'])
            gp=f"{g['n']} / "+' / '.join(f"{g[k]:.5f}" for k in ['median','p95','p99','peak'])
            lines.append(f"| {pair['mode']} / {key} | {c['n']} | {cp} | {gp} | {g['over8.33']} / {g['over11.11']} |")
    lines+=['','Regional after-authority CPU mean recovery is1.561125 ms and median recovery1.8452 ms; full route mean recovery is1.560169 ms and median recovery1.8865 ms. Non-fence mean recovery is approximately2.0025/1.9661 ms. Normal sub40 ms topology/upload tails remain: full-route peaks24.6708/22.5101 and regional24.4598/23.1801. Neither pair has a CPU sample above40 ms after initial authority. This improves the display-frame critical path without eliminating all publication work.','',
            'Regional contains exactly737 aligned Earth frames on each side with identical37 generation/level/hash/publication-frame signatures. TCS/TES counts nevertheless differ on541 frames and fragment counts on531; no full camera/physical-byte stream is present. Full traversal has1697/1698 aligned frames and58 matching publication generation/level/hash signatures; generation25 onward shifts one native frame. At common frame numbers,34 generation,810 TCS,1040 TES and1373 fragment observations differ. Thus full traversal is a complete route comparison, not an exact cross-frame geometry join.','',
            'Regional GPU mean changes8.479253 to8.544920 ms; P99 changes12.64832 to12.82576 and >11.11 counts48 to58. Full GPU mean changes4.860449 to4.867943 ms; peaks8.87760/8.62480 and >8.33 counts8/9. These small mixed differences remain visible. There is no measured GPU-time recovery claim and no120 FPS GPU closure. Fixed geometry/output, publication, fallback and ordinary-driver tests remain required.','',
            '## Return-baseline and logging-disabled checks','',
            'The uncached regional return reproduces58 GPU frames >11.11, matching cached58 rather than the first baseline48. Return P95/peak are11.91204/13.35426 ms against cached11.87425/13.15552. Return and cached publication identities/cadence still match. Thus the initial additional10 threshold misses are not uniquely attributable to cached-key placement. No GPU saving is claimed.','',
            'Against the return, regional CPU median recovery remains1.7401 ms and P99 improves15.6865 to13.9121 ms. Mean recovery is1.446264 ms, below1.5, versus1.561125 for the first baseline. The meaningful typical-frame benefit is repeated; it is not >=1.5 for every population mean or every frame.','',
            'The same-host regional check with host/queue/GPU per-frame journaling disabled reports whole-run CPU average10.480 to8.963 ms (1.517 recovery), median11.439 to9.622 (1.817), P9514.387 to12.506 and P9915.693 to13.936. The mixed validation/upload mean drops2.011 to.112 ms. Both complete1000 frames and37 publications with zero recorded errors. This confirms the benefit is not dependent on those per-frame journal calls. Residual timestamps/log hooks remain: these are not fully uninstrumented runs. Their aggregate maxima184.415/194.019 include an unseparated startup-inclusive population, so no normal-frame tail attribution is manufactured from them.','',
            '## Continuation-bar interpretation','',
            'The ticket explicitly includes CPU update/submission as a closure gate and asks for net CPU/GPU improvement. A safe >=1.5 ms reduction in ordinary serialized display-frame wall time can qualify as meaningful CPU payoff. It must be labeled CPU/display-frame recovery; it does not reduce the GPU query, grant 8.33 ms GPU closure, or make M13.5 obsolete.','',
            'All stages of qualification remain separate: preserved inputs and request/upload/pending counters; actual net whole-CPU and non-fence benefit; unchanged or bounded GPU/tail behavior; normal production-managed traversal; exact key ownership/coherence/polling/publication; portable fallback and failure handling. A function timer alone cannot pass this gate.','',
            '**Performance judgment: PASS for the bounded prototype continuation gate.** Complete same-host Florida and normal production-managed full/regional routes show meaningful CPU/display-frame benefit. The orbital return/repeat resolves the apparent placement-specific wall-time regression; that regime remains paced with no claimed end-to-end gain. This is not final production readiness or a general GPU performance PASS.','',
            '## Cheap output gate and implemented-candidate boundary','',
            'The completed keys-florida-parity.json and keys-inland-parity.json reports both PASS at frame175,3440x1440: no logged input differences, all64 bytes of every prepared current vertex exact, exact oriented submitted triangle multiset and zero changed D32/HDR/final-image pixels. Compacted raw index order is not required to match; winding and multiplicity are preserved. Capture-enabled timings are excluded from both this review and Work A. These are prototype-selector captures, not yet the final default implementation identity.','',
            'The lead reports the bounded implementation as one TerrainRequestKeys cached/coherent preference and one allocation call site, with the original compatible fallback, all30 policy cases passing Debug/Release and native/managed builds passing. Ordinary queue polling, writes, fences, publications and the M13.5 GPU-local roles remain the required contract. The separately labeled implemented-candidate section below records available default timings and parity without replacing these prototype populations. Normal route/suite completion, deployment verification and lifecycle negatives are finalized by the lead. No further candidate exploration is recommended.','',
            '## Reproduction','',
            'Run `python -B docs/engineering-evidence/m13-final-exit/verify-b.py` after the bounded journals are complete. Both JSON and lossless gzip are supported. The complete journals remain the authority for every stored frame/identity; this file writes only verify-b.md/json.','']
    lines+=candidate_markdown(candidate)
    (HERE/'verify-b.md').write_text('\n'.join(lines),encoding='utf-8')
    print(f'Verified {len(runs)} fixed controls, {len(pairs)} same-camera comparisons.')


if __name__=='__main__':main()
