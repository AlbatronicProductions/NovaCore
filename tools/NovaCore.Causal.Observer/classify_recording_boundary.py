"""Frame-168 retained-run audit; not a general journal parser. Never loads Vulkan."""
import argparse, hashlib, json, struct
from pathlib import Path

def inspect(run, output):
    run=run.resolve();output=output.resolve()
    assert output!=run and run not in output.parents, 'Preserve the original evidence directory'
    output.mkdir(parents=True, exist_ok=True)
    blob=(run/'breadcrumbs.bin').read_bytes()
    header=struct.unpack_from('<16Q',blob)
    assert header[:3]==(0x314c41535541434e,2,512)
    rows=[]
    for offset in range(128,len(blob),512):
        data=blob[offset:offset+512]; w=struct.unpack('<64Q',data)
        if not w[0]: continue
        checksum=14695981039346656037
        for i,b in enumerate(data[:504]):
            if not 488<=i<496: checksum=((checksum^b)*1099511628211)&((1<<64)-1)
        assert w[0]==w[63] and checksum==w[61], 'Record integrity failure'
        rows.append(w)
    rows.sort()
    assert [w[0] for w in rows]==list(range(1,header[6]+1))
    assert header[6]==header[7]==header[8] and header[11]==header[12]==0
    names=['','Setup','Device','Swap','Recreate','Fence','Host','Upload','Record','Acquire','Submit','Present','Cleanup','Fault','Snapshot','Resource','Dispatch','Draw','Capabilities','Completed','MemoryBudget','Validation','DeviceIdle','QueueIdle','FenceApi','DiagnosticStop','FrameAuthority','FrozenCapture']
    def record(w):return dict(serial=w[0],qpc=w[1],frame=w[2],submitted=w[3],completed=w[4],phase=names[w[5]],kind=w[6],result=struct.unpack('<q',struct.pack('<Q',w[7]))[0],swap=w[8],producerThreadId=w[62],data=list(w[9:61]))
    summary=json.loads((run/'summary.json').read_text())
    submit=[w for w in rows if w[5:7]==(10,1) and w[7]==0]
    frame_submit=[w for w in submit if w[2]>0]
    assert [w[2] for w in frame_submit]==list(range(1,168))
    assert [w[11] for w in submit]==list(range(1,231))
    submit_begin=next(w for w in rows if w[0]==submit[-1][0]-1)
    assert submit_begin[5:7]==(10,0) and submit_begin[13]==submit[-1][11]
    waits=[w for w in rows if w[5:7]==(24,1) and w[7]==0]
    wait_begin=next(w for w in rows if w[0]==waits[-1][0]-1)
    assert wait_begin[5:7]==(24,0) and wait_begin[9]==1
    assert wait_begin[10]==submit[-1][10], 'Last completed fence must match last submitted fence'
    complete=[w for w in rows if w[5:7]==(19,2) and w[4]>0]
    begin=next(w for w in rows if w[2]==168 and w[5:7]==(8,0))
    assert complete[-1][4]==167 and frame_submit[-1][3]==167
    frozen=[w for w in rows if w[5]==27]
    assert len(frozen)==1 and frozen[0][9:13]==(1,2,88*1024*1024,1000)
    assert not any(w[5]==26 for w in rows)
    counters=[w for w in rows if w[5:7]==(14,2) and w[9] in (0,1,3,4,5) and w[2] in (166,167,168)]
    ledger=json.loads((run/'resource-lifetimes.json').read_text())
    resources=ledger['history']
    large_readback=[r for r in resources if r['Kind']==3 and r['Facts'][0]==88*1024*1024]
    terrain=[r for r in resources if r['BirthSerial']>=6681]
    assert len(large_readback)==2 and all(r['DeathSerial'] is None for r in large_readback)
    assert all(r['DeathSerial'] is None for r in terrain)
    final=rows[-1]; stop=summary['firstStopDecisionQpc']; freq=header[5]
    camera=next(w for w in counters if w[2]==168 and w[9]==0)
    scalar=lambda bits:struct.unpack('<d',struct.pack('<Q',bits))[0]
    artifact=dict(schema=1,gpuExecutionThisTool=False,journalSha256=hashlib.sha256(blob).hexdigest(),
        header=dict(zip(['magic','version','recordBytes','ringCapacity','producerPid','qpcFrequency','emitted','received','durable','stopRequested','observerReady','overruns','contentionDrops','lastProducerQpc','lastObserverHeartbeatQpc','journalCapacity'],header)),
        successfulQueueSubmissions=len(submit),bootstrapSubmissions=len(submit)-len(frame_submit),lastSubmissionBegin=record(submit_begin),lastSubmission=record(submit[-1]),
        lastSuccessfulFenceWaitBegin=record(wait_begin),lastSuccessfulFenceWait=record(waits[-1]),lastFrameCompletion=record(complete[-1]),recordingBegin=record(begin),lastProducerEvent=record(final),
        lastFenceWaitMs=(waits[-1][1]-wait_begin[1])*1000/freq,
        heartbeatAfterLastProducerMs=(header[14]-header[13])*1000/freq,stopAfterRecordBeginMs=(stop-begin[1])*1000/freq,
        lastHeartbeatAfterStopMs=(header[14]-stop)*1000/freq,stopQpc=stop,
        actualReadbackEvents=[record(w) for w in frozen],frameAuthorityRecords=0,
        cameraBeforeRecord=dict(bodyPositionMetres=[scalar(camera[9+i]) for i in (4,5,6)],altitudeMetres=scalar(camera[16]),forward=[scalar(camera[9+i]) for i in (8,9,10)],viewport=[camera[10],camera[11]],exactFrozenProjection='NOT OBSERVED'),
        memoryBudgetSamples=[record(w) for w in rows if w[5:7]==(20,2)],
        frames167168=[record(w) for w in rows if w[2] in (167,168) and w[5]!=14],
        terrainSnapshots=[record(w) for w in counters],readbackResources=large_readback,terrainResourceBirths=terrain,
        classification=dict(observed='Command recording did not return before first capture scheduling/submission',
            previousSubmittedFrameCompleted=True,frame168Submitted=False,readbackGpuCompletionPending=False,
            slot0='Free by production state-transition proof',slot1='Free or first Reserved; reserve boundary was not recorded',
            possibleFirstCaptureIdentity=1,possibleFirstCaptureSlot=1,writerOwnsEitherSlot=False,
            writerReuseBlocked=False,writerWaitingForGpu=False,
            exactCpuCall='NOT OBSERVED; final draw through frozen recording tail',
            proofAssumptions='Sealed source matches runtime; intact causal stream; normal ownership invariants, without unobserved memory corruption'),
        sourceEvidence=dict(summarySha256=hashlib.sha256((run/'summary.json').read_bytes()).hexdigest(),resourceLedgerSha256=hashlib.sha256((run/'resource-lifetimes.json').read_bytes()).hexdigest()))
    (output/'recording-boundary.json').write_text(json.dumps(artifact,indent=2)+'\n')
    print(json.dumps({k:artifact[k] for k in ('successfulQueueSubmissions','bootstrapSubmissions','heartbeatAfterLastProducerMs','stopAfterRecordBeginMs','lastHeartbeatAfterStopMs','classification')}))

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('run',type=Path);p.add_argument('output',type=Path);a=p.parse_args();inspect(a.run,a.output)
