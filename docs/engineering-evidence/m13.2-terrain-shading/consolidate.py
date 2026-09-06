"""Consolidate this ticket's measured runs; never regenerate raw data."""
import sys
sys.dont_write_bytecode=True
import hashlib,json,re
from run import ROOT,HERE,OUT
from analyze import fields,stats

def write(name,value): (HERE/name).write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')

def consolidate():
    runs=[]
    for path in sorted(OUT.glob('*.json')):
        data=json.loads(path.read_text(encoding='utf-8'))
        if not isinstance(data,dict) or 'captureMode' not in data: continue
        text=(OUT/(path.stem+'.log')).read_text(encoding='utf-8')
        frames=[fields(line) for line in text.splitlines() if 'P2S5F directional visibility:' in line][-100:]
        data['factorHistogramSamples']=sorted(set(str(frame['innerFactorBins']) for frame in frames))
        data['captureFrameCounters']=[fields(line) for line in text.splitlines() if 'P2S5F directional visibility:' in line and fields(line)['submittedFrame']==int(data['environment'].get('NOVACORE_PARITY_FRAME',175))]
        data['totalOutsideCullAndTerrainMs']=stats([f['gpuTotalMs']-f['gpuCullCompactMs']-f['gpuDetailedDrawMs'] for f in frames])
        data['validationMessages']=list(dict.fromkeys(line for line in text.splitlines() if 'Vulkan validation [' in line))
        runs.append(data)
    by={d['name']:d for d in runs};parity=[];performance=[];workload=[]
    for pose in 'ABCDE':
        a,b=[by[f'parity-{pose}-{probe}'] for probe in ('baseline','candidate')]
        comparison=b['captureAnalysis']['versusBaseline']
        assert all(comparison[k]['exact'] for k in ('depth','hdr','image')) and comparison['physicalRecordSetExact']
        assert a['rawFiles']['prepared.bin']==b['rawFiles']['prepared.bin']
        assert a['factorHistogramSamples']==b['factorHistogramSamples']
        parity.append(dict(pose=pose,baselineExecutedRecords=a['captureAnalysis']['tesRecords'],candidateExecutedRecords=b['captureAnalysis']['tesRecords'],uniquePhysicalDirections=a['captureAnalysis']['uniqueDirections'],physicalSetSha256=a['captureAnalysis']['physicalRecordSetSha256'],preparedData=a['rawFiles']['prepared.bin'],comparison=comparison,depthValues=3440*1440,hdrHalfValues=3440*1440*4,finalImageByteValues=3440*1440*4))
        a,b=[by[f'timing-{pose}-{probe}'] for probe in ('baseline','candidate')]
        am,bm=a['metrics'],b['metrics']
        gain=am['gpuDetailedDrawMs']['median']-bm['gpuDetailedDrawMs']['median']
        performance.append(dict(pose=pose,baselineTerrain=am['gpuDetailedDrawMs'],candidateTerrain=bm['gpuDetailedDrawMs'],improvementMs=gain,improvementPercent=100*gain/am['gpuDetailedDrawMs']['median'],baselineTotalGpu=am['gpuTotalMs'],candidateTotalGpu=bm['gpuTotalMs']))
        keys=['compactedTriangles','tcsPatches','refinedVertices','clippingInputPrimitives','clippingOutputPrimitives','fragmentInvocations','tesEnvelopeActive','tesEnvelopeInactive','maximumOuterTesFactor','maximumInnerTesFactor']
        workload.append(dict(pose=pose,baseline={k:am[k] for k in keys},candidate={k:bm[k] for k in keys},factorBins=a['factorHistogramSamples']))
        for key in ('compactedTriangles','tcsPatches','tesEnvelopeActive','tesEnvelopeInactive','maximumOuterTesFactor','maximumInnerTesFactor'):
            assert am[key]==bm[key],(pose,key)
        assert a['factorHistogramSamples']==b['factorHistogramSamples']
    for diagnostic in ('owners','boundaries'):
        good=by[f'diagnostic-{diagnostic}-candidate']['captureAnalysis']['versusBaseline']
        negative=by[f'diagnostic-{diagnostic}-negative']['captureAnalysis']['versusBaseline']
        assert all(good[k]['exact'] for k in ('depth','hdr','image')) and good['physicalRecordSetExact']
        assert negative['depth']['exact'] and negative['physicalRecordSetExact'] and not negative['hdr']['exact'] and not negative['image']['exact']
    for label in ('bootstrap','physical-modifier'):
        good=by[label+'-candidate']['captureAnalysis']['versusBaseline']
        assert all(good[k]['exact'] for k in ('depth','hdr','image'))
    write('measurements.json',runs)
    write('results.json',dict(performance=performance,parity=parity,workload=workload,rawBytesCreatedAndRemoved=sum(item['bytes'] for d in runs for item in d.get('rawFiles',{}).values())))
    print('Consolidated',len(runs),'runs; exact parity and workload contracts PASS')

if __name__=='__main__': consolidate()
