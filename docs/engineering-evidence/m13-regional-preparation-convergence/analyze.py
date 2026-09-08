"""Recompute matched-event statistics from lossless compact journals."""
import pathlib,json,gzip,math,statistics
HERE=pathlib.Path(__file__).resolve().parent
TARGET=list(range(345,356))+list(range(357,368))+list(range(725,736))+list(range(737,748))
def load(name):
    p=HERE/(name+'.json')
    return json.loads(p.read_bytes() if p.exists() else gzip.decompress(p.with_suffix('.json.gz').read_bytes()))
def fields(line):
    return dict(item.strip().split('=',1) for item in line.split(': ',1)[1].split(';') if '=' in item)
def unpack(value):
    if isinstance(value,list):return value
    return [dict(value['constants'],**{k:v[i] for k,v in value['columns'].items()}) for i in range(value['count'])]
def stats(values):
    s=sorted(values)
    if not s:return None
    at=lambda q:s[min(len(s)-1,math.ceil(q*len(s))-1)]
    return dict(count=len(s),mean=statistics.mean(s),median=statistics.median(s),p95=at(.95),p99=at(.99),peak=s[-1])
def physical(bits):
    b=bytes.fromhex(bits)
    return (b[:128]+b[136:144]+b[152:156]).hex()
def correlate(d):
    logical=0;frames={};journal={}
    for line in d['lines']:
        if 'Composition route frame:' in line:logical=int(fields(line)['logicalFrame'])
        if 'Composition frame:' in line and logical:
            f=fields(line);frame=int(f['frame']);f['logical']=logical;frames[frame]=logical;journal[logical]=f
    host=unpack(d['hostRows']);render={}
    for row in host:
        if row['gpuFrame'] in frames and row['geometryFrame']==row['gpuFrame']:
            render[frames[row['gpuFrame']]]=row
    return frames,journal,render
def main():
    output={'targetLogicalFrames':TARGET,'percentiles':'nearest rank; all selected tails included','modes':{},'oracle':{}}
    baseline=correlate(load('A'))
    for mode in ['A','B','C','D','D-mapped']:
        d=load(mode);frames,journal,render=correlate(d)
        target=[render[i] for i in TARGET]
        prep={int(x['frame']):x['ms'] for x in d['preparation']['incomingPhysicalPreparation']}
        public=[fields(x) for x in d['lines'] if 'Prep publication:' in x]
        physicalMatch={}
        comparisons={'camera':'cameraBits','gpuConstants':'gpuBits','presentation':'presentationBits','lighting':'lightingBits',
          'published':'publishedBits','incoming':'incomingBits','currentTopology':'currentTopology','incomingTopology':'incomingTopology',
          'generation':'generation','incomingGeneration':'incomingGeneration','cursor':'cursor','active':'active','completeDependencies':'completeDependencies'}
        for key,column in comparisons.items():
            different=[]
            for i in sorted(journal.keys()&baseline[1].keys()):
                a=baseline[1][i][column];b=journal[i][column]
                if column in ['publishedBits','incomingBits']:a=physical(a);b=physical(b)
                if a!=b:different.append(i)
            physicalMatch[key]={'differentLogicalFrames':different,'target44Exact':not set(TARGET)&set(different)}
        tcs=[i for i in sorted(render.keys()&baseline[2].keys()) if render[i]['tcsPatches']!=baseline[2][i]['tcsPatches'] or render[i]['tesInvocations']!=baseline[2][i]['tesInvocations']]
        maps=[fields(x) for x in d['lines'] if 'Composition map:' in x]
        output['modes'][mode]=dict(exitCode=d['exitCode'],errors=d['errors'],nativeFrames=len(unpack(d['hostRows'])),
          logicalFrames=len(journal),publications=len(public),transition=stats([r['gpuTotal'] for r in target]),
          incomingSlice=stats([prep[r['gpuFrame']] for r in target]),
          cpuAtCompletion={k:stats([r[k] for r in target]) for k in ['update','hostCallback','validationUpload','record','submit','fenceWait','inspection','present','total']},
          cpuAtPreparation={k:stats([next(x for x in unpack(d['hostRows']) if x['frame']==r['gpuFrame'])[k] for r in target]) for k in ['update','hostCallback','validationUpload','record','submit','fenceWait','inspection','present','total']},
          targetPublications=[p for p in public if int(p['generation']) in [18,19,36,37]],
          equivalence=physicalMatch,tcsTesDifferentLogicalFrames=tcs,
          normalAlignedEarthGpu=stats([r['gpuTotal'] for r in render.values()]),
          normalGpuTails=[dict(logical=i,gpuMs=r['gpuTotal'],incomingMs=prep.get(r['gpuFrame'])) for i,r in render.items() if r['gpuTotal']>11.11],
          diagnosticMapCpu=stats([float(m['diagnosticSynchronousCpuMs']) for m in maps]),mapRows=maps)
    for mode in ['D-oracle','D-invalid-source','D-mapped-oracle']:
        try:d=load(mode)
        except FileNotFoundError:continue
        rows=[fields(x) for x in d['lines'] if 'Prep GPU oracle:' in x]
        negatives=[fields(x) for x in d['lines'] if 'Composition negative oracle:' in x]
        mapped=[fields(x) for x in d['lines'] if 'Composition mapped oracle:' in x]
        total=sum(int(x['total']) for x in rows);reused=sum(int(x['exactDirectionReuse']) for x in rows)
        output['oracle'][mode]=dict(exitCode=d['exitCode'],errors=d['errors'],slices=len(rows),
          publications=len({x['generation'] for x in rows}),totalRecords=total,proposedCopies=reused,
          fallbackRecords=total-reused,maximumMismatchCounter=max(int(x['physicalWordMismatches']) for x in rows),
          copySourceBytes=reused*64,destinationWriteBytes=total*64,
          negatives={k:sum(int(x[k]) for x in negatives) for k in ['regionalContributionChanged','supportContributionChanged','sourceIdentityRejected','basisChanged','invalidAccepted']},
          mappedSourceRecords=sum(int(x['mapped']) for x in mapped),targetSlices=[x for x in rows if int(x['generation']) in [18,19,36,37]],
          targetMappedSlices=[x for x in mapped if int(x['generation']) in [18,19,36,37]])
    (HERE/'analysis.json').write_text(json.dumps(output,indent=2)+'\n')
    for mode,m in output['modes'].items():print(mode,m['transition'],m['incomingSlice'])
    for mode,m in output['oracle'].items():print(mode,{k:v for k,v in m.items() if k not in ['targetSlices','targetMappedSlices']})
if __name__=='__main__':main()
