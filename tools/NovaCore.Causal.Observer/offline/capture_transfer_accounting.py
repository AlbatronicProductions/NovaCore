"""Read-only accounting of retained capture bytes and measured fence/GPU cost.

Writes only the explicitly named report. Does not launch an application/GPU.
Transport predictions are byte counts, not predicted hardware timings.
"""
import argparse, hashlib, json, math, statistics, struct
from pathlib import Path

def digest(path):
    with path.open('rb') as file: return hashlib.file_digest(file, 'sha256').hexdigest()

def stats(values):
    values=sorted(values)
    if not values: return {'samples':0}
    return dict(samples=len(values),median=statistics.median(values),p95=values[math.ceil(.95*len(values))-1],p99=values[math.ceil(.99*len(values))-1],maximum=max(values))

def inspect(run):
    ledger=json.loads((run/'resource-lifetimes.json').read_text())['history']
    snapshots=[]
    for path in sorted((run/'frozen').glob('snapshot-*.bin')):
        with path.open('rb') as f:
            h=f.read(4096);w=struct.unpack_from('<64Q',h); sections=[struct.unpack_from('<4Q',h,512+32*i) for i in range(6)]
            f.seek(sections[5][1]);draw=struct.unpack('<5I',f.read(20))
        v,t=w[18:20]; cold=64*v+16*t+188;warm=64*v+4*t+188;old=64*v+28*t+188
        sizes=[64*v,12*t,4*t,12*t,168,20]
        buffers=[]
        for i in range(6):
            lives=[life for life in ledger if life['Kind']==3 and life['Handle']==w[32+i] and life['BirthFrame']<=w[4]]
            assert lives
            life=lives[-1]
            buffers.append(dict(section=i+1,handle=w[32+i],sourceBytes=life['Facts'][0],oldCopiedBytes=sizes[i],newColdGpuBytes=0 if i==3 else sizes[i],newWarmGpuBytes=0 if i in (1,3) else sizes[i],slotDestinationOffset=sections[i][1]-4096,birthSerial=life['BirthSerial'],deathSerial=life['DeathSerial']))
        snapshots.append(dict(file=str(path),sha256=digest(path),frame=w[4],submission=w[5],generation=w[11],pupil=w[15],topologyHash=w[13],vertices=v,triangles=t,visible=draw[0]//3,vertexCapacity=w[38],triangleCapacity=w[39],
            oldGpuBytes=old,newColdGpuBytes=cold,newWarmGpuBytes=warm,newHostCompactedBytes=draw[0]*4,oldCompactedUnwrittenTailBytes=12*t-draw[0]*4,
            oldGpuCopyCalls=6,newColdGpuCopyCalls=5,newWarmGpuCopyCalls=4,barriers=2,slotBytes=88*1024*1024,slots=2,fileBytes=path.stat().st_size,buffers=buffers))
    with (run/'breadcrumbs.bin').open('rb') as f:
        head=struct.unpack('<16q',f.read(128));rows=[]
        for i in range(head[15]):
            raw=f.read(512);w=struct.unpack('<64Q',raw)
            if w[0]: rows.append((w,raw))
    rows.sort(key=lambda r:r[0][0]);captureFrames=set()
    transfer=[];cpu=[];fences=[];allGpu={};fenceBegin=None
    for w,raw in rows:
        if w[5]==27 and w[6]==2 and w[9]==2: captureFrames.add(w[11])
        if w[5]==27 and w[6]==2 and w[9]==6: transfer.append(struct.unpack_from('<d',raw,13*8)[0])
        if w[5]==27 and w[6]==2 and w[9]==7 and w[11]: cpu.append(struct.unpack_from('<d',raw,12*8)[0])
        if w[5]==24 and w[6]==0:fenceBegin=w[1]
        if w[5]==24 and w[6]==1 and fenceBegin is not None:
            fences.append(dict(frame=w[3],serial=w[0],capture=w[3] in captureFrames,milliseconds=(w[1]-fenceBegin)*1000/head[5]));fenceBegin=None
        if w[5]==14 and w[6]==2 and w[9]==7:allGpu[w[10]]=struct.unpack_from('<d',raw,11*8)[0]
    return dict(run=str(run),journalSha256=digest(run/'breadcrumbs.bin'),snapshots=snapshots,
        retainedCaptureFenceMilliseconds=stats([v['milliseconds'] for v in fences if v['capture']]),noncaptureFenceMilliseconds=stats([v['milliseconds'] for v in fences if not v['capture']]),
        gpuTransferBarrierMilliseconds=stats(transfer),cpuRecordingMilliseconds=stats(cpu),fullCaptureGpuMilliseconds=stats([ms for frame,ms in allGpu.items() if frame in captureFrames]),captureFences=[f for f in fences if f['capture']])

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--run',type=Path,action='append',required=True);parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    result=dict(gpuExecution=False,scope='Retained hardware measurements; source-derived transport byte plan, no claim of new hardware timing',runs=[inspect(run) for run in args.run])
    args.output.write_text(json.dumps(result,indent=2)+'\n');print('Retained transfer accounting written:',args.output)
