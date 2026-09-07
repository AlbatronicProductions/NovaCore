"""Derive report tables from retained full numerical evidence."""
import json,pathlib,re,hashlib,sys
sys.dont_write_bytecode=True
from records import read
from run import stats
P=pathlib.Path(__file__).resolve().parent
def main():
    output={'performance':[],'cpu':[],'baselineWorkload':[],'preparation':[],'outliers':[],'compiler':{}}
    for pose in ['orbital','factor1','florida','active','grazing']:
        a=read(P/f'final-{pose}-baseline.json');b=read(P/f'final-{pose}-candidate.json')
        am=a['analysis']['metrics'];bm=b['analysis']['metrics']
        row={'pose':pose,'baselineTerrain':am['gpuDetailedDrawMs'],'candidateTerrain':bm['gpuDetailedDrawMs'],'baselineGpu':am['gpuTotalMs'],'candidateGpu':bm['gpuTotalMs'],'savedGpuMs':am['gpuTotalMs']['median']-bm['gpuTotalMs']['median'],'candidateAbove8_33':bm['gpuTotalMs']['median']-8.33,'candidateAbove11_11':bm['gpuTotalMs']['median']-11.11}
        output['performance'].append(row)
        a=read(P/f'cpu-{pose}-baseline.json');b=read(P/f'cpu-{pose}-candidate.json')
        output['cpu'].append({'pose':pose,'baseline':{k:v for k,v in a['analysis']['metrics'].items() if k.startswith('host_')},'candidate':{k:v for k,v in b['analysis']['metrics'].items() if k.startswith('host_')}})
        for candidate,data in [('baseline',a),('candidate',b)]:
            groups={}
            for kind,info in data['preparation'].items():
                settled=[x for x in info['rows'] if x['frame']>=data['frameRows'][0]['submittedFrame']]
                groups[kind]={'sumMs':info['sumMs'],'slices':info['slices'],'cost':info['cost'],'settledSlices':len(settled),'settledMs':sum(x['ms'] for x in settled)}
            output['preparation'].append({'pose':pose,'mode':candidate,'groups':groups,'provenance':[x for x in data['identity'] if any(v in x for v in ['regional physical totals:','regional ready:','spherical billboard publication:'])]})
            output['outliers'].extend({'label':data['label'],**r} for r in data['hostRows'] if r['total']>=40)
        data=read(P/f'baseline-{pose}.json');m=data['analysis']['metrics'];f=data['frameRows'][-1]
        output['baselineWorkload'].append({'pose':pose,'terrain':m['gpuDetailedDrawMs'],'gpu':m['gpuTotalMs'],'tcsPatches':f['tcsPatches'],'tesInvocations':f['refinedVertices'],'clipInput':m['clippingInputPrimitives'],'clipOutput':f['clippingOutputPrimitives'],'fragments':f['fragmentInvocations'],'factorBins':f['innerFactorBins'],'maximumFactor':f['maximumOuterTesFactor']})
    for label in ['baseline-active','cheap-active','zero-active']:
        stages={}
        for path in (P/(label+'-compiler')).glob('*.isa'):
            text=path.read_text(errors='replace');lines=[x for x in text.splitlines() if re.match(r'\s+[a-z][a-z0-9_]+\s',x) and ';' in x and 's_code_end' not in x];words=[x.split(';')[-1].strip() for x in lines]
            stages[path.stem]={'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'machineWordsSha256':hashlib.sha256('\n'.join(words).encode()).hexdigest(),'instructions':len(lines),'fp64Named':sum('_f64' in x for x in lines),'fp32Named':sum('_f32' in x for x in lines),'wave32':'UC_VERSION_W32_BIT' in text}
        data=read(P/(label+'.json'));output['compiler'][label]={'stages':stages,'resources':[x for x in data['identity'] if 'AMD shader statistics:' in x]}
    (P/'summary.json').write_text(json.dumps(output,indent=2)+'\n')
    print('summary.json written from full retained samples')
if __name__=='__main__':main()
