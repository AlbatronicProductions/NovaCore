import pathlib,json,struct,hashlib,collections,math,shutil,os,re,statistics
out=pathlib.Path(__file__).resolve().parents[4]/'build/ordinary-recorder-benign-revision/live-retry'
def write(n,v):(out/n).write_text(json.dumps(v,indent=2)+'\n')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
owner=json.loads((out/'owner.json').read_text());session=json.loads((out/'session.json').read_text());src=pathlib.Path(owner['sessionDirectory'])
dst=out/'retained-session'
if not dst.exists():shutil.copytree(src,dst)
for p in src.iterdir():
 if p.is_file():assert sha(p)==sha(dst/p.name),p.name
logs=pathlib.Path(os.environ['LOCALAPPDATA'])/'NovaCore/Logs'
matches=[p for p in logs.iterdir() if f'-{owner["pid"]}-' in p.name];assert len(matches)==1
if not (out/'application-log').exists():shutil.copytree(matches[0],out/'application-log')
raw=(out/'live-events.bin').read_bytes();assert len(raw)%256==0
events=list(struct.iter_unpack('<32Q',raw));freq=session['qpcFrequency']
counts=collections.Counter();pending={};resources={};births={};opens={};phases=collections.defaultdict(list);frameStarts=[];frameOrder={};errors=[];generations=set();lastbirth=0;completed=[]
def valididentity(kind,handle,birth):
 if not handle:return birth==0
 return resources.get((kind,handle))==birth
for idx,e in enumerate(events,1):
 assert e[0]==e[31]==idx,('sequence',idx)
 h=14695981039346656037
 for b in raw[(idx-1)*256:(idx-1)*256+240]:h=((h^b)*1099511628211)&0xffffffffffffffff
 assert h==e[30],('checksum',idx)
 assert e[29]==0,('fault',idx,e[29])
 phase,edge=e[4:6];counts[f'{phase}:{edge}']+=1;generations.add((e[10],e[11],e[12]));key=(phase,e[7])
 if edge==1:
  assert key not in opens,('duplicate-entry',idx);opens[key]=e
 elif edge in (2,3):
  assert key in opens,('unmatched-return',idx)
  entry=opens.pop(key);assert e[1]>=entry[1];phases[phase].append((e[1]-entry[1])*1000/freq)
  assert edge!=3 and not(e[6]>>63),('error-return',idx)
 if phase==2 and edge==1:frameStarts.append(e[1])
 if phase in (2,3,4):frameOrder.setdefault(e[2],[]).append((phase,edge))
 if phase==12 and edge==0:
  kind,handle,birth,action=e[18],e[13],e[14],e[17]
  if action==1:
   assert (kind,handle) not in resources and birth>lastbirth
   resources[kind,handle]=birth;births[birth]=(kind,handle);lastbirth=birth
  elif action==2:
   assert valididentity(kind,handle,birth),('retirement',idx);del resources[kind,handle]
  else:
   assert valididentity(kind,handle,birth),('binding/mapping',idx)
   assert valididentity(1,e[15],e[16]),('memory',idx)
 if phase==13 and e[17]:assert valididentity(e[18],e[13],e[14]),('role',idx)
 if phase==7 and edge==1:
  assert valididentity(4,e[13],e[14]) and valididentity(5,e[15],e[16]) and valididentity(6,e[18],e[27]),('submit',idx)
 if phase==7 and edge==2 and e[6]==0:
  assert e[8] not in pending;pending[e[8]]=e
 if phase==9:
  assert e[8] in pending,('unproven-completion',idx)
  submit=pending.pop(e[8]);assert e[13:28]==submit[13:28],('completion-context',idx);completed.append(e[8])
assert not opens and not pending and not resources,(len(opens),len(pending),len(resources))
for loop,order in frameOrder.items():assert order==[(2,1),(3,1),(3,2),(4,1),(4,2),(2,2)],('frame-order',loop,order)
def quant(v):
 v=sorted(v)
 return {'n':len(v),**{k:v[min(len(v)-1,math.ceil(p*len(v))-1)] for k,p in [('p50',.5),('p95',.95),('p99',.99),('max',1)]}} if v else {}
# Independently validate final committed head/page authority and compare every
# retained event after the checkpoint byte-for-byte to the live read-only copy.
heads=[(dst/f'head{i}.bin').read_bytes() for i in (0,1)]
for h in heads:assert hashlib.sha256(h[:-32]).digest()==h[-32:]
get=lambda b,i:struct.unpack_from('<Q',b,i)[0]
head=max(heads,key=lambda b:get(b,24));bank=(dst/f'bank{get(head,32)}.bin').read_bytes();checkpoint=get(head,48);seq=checkpoint
assert hashlib.sha256(bank[:4096-32]).digest()==bank[4096-32:4096]
assert hashlib.sha256(bank[4096:4096+get(head,72)]).digest()==head[80:112]
for pi in range(get(head,64)):
 page=bank[4096+32*1024*1024+pi*4096:4096+32*1024*1024+(pi+1)*4096]
 assert hashlib.sha256(page[:224]).digest()==page[224:256]
 assert get(page,16)==get(head,40) and get(page,24)==seq+1
 for ri in range(get(page,32)):
  b=page[256+ri*256:512+ri*256];assert b==raw[seq*256:(seq+1)*256];seq+=1
recovery=json.loads((dst/'recovery.json').read_text());assert seq==get(head,56)==recovery['DurableSequence']==len(events)
assert recovery['Clean'] and recovery['Faults']==0 and not recovery['Corrupt'] and recovery['Dropped']==0
water=[json.loads(line) for line in (out/'watermarks.jsonl').read_text().splitlines()]
frames=[(b-a)*1000/freq for a,b in zip(frameStarts,frameStarts[1:])]
performance=json.loads((dst/'performance.json').read_text())
assert recovery['Complete'] and recovery['Terminal'].startswith('COMPLETE')
assert events[-1][4:7]==(20,2,0) and events[-1][17]==1 and events[-1][18]==seq
assert events[0][4]==1 and next(i for i,e in enumerate(events) if e[4]==12)>0
assert json.loads((out/'watch-result.json').read_text())['judgment']=='COMPLETED'
cost=json.loads((out/'route-cost.json').read_text());samples=cost['samples'];header=cost['header']
assert header['Overflow']==0 and header['Frequency']==freq and header['Count']==len(samples)==cost['heldFrames']
assert all(s['AllocationCalls']==s['AllocationBytes']==0 for s in samples)
markers=[e for e in events if e[4]==19 and e[17]==1]
assert len(markers)==len(cost['coverage']) and len(markers)>=3
submits={e[2]:e for e in events if e[4]==7 and e[5]==2 and e[6]==0 and e[2]}
completions={e[8]:e for e in events if e[4]==9}
presents={e[2]:e for e in events if e[4]==10 and e[5]==2 and e[6]==0 and e[2]}
entries={e[2]:e for e in events if e[4]==2 and e[5]==1}
returns={e[2]:e for e in events if e[4]==2 and e[5]==2}
joins=[]
for marker,coverage in zip(markers,cost['coverage']):
 loop=marker[2];v=coverage['value'];distance=struct.unpack('<d',struct.pack('<Q',marker[19]))[0];radius=struct.unpack('<d',struct.pack('<Q',marker[20]))[0]
 assert marker[18]==v['FocusBody']==v['SubmittedBody']==6 and distance==v['Distance'] and radius==v['Radius']
 assert distance>=4*radius and abs(distance-v['ExpectedDistance'])<1 and abs(distance-v['SubmittedDistance'])<1
 assert not(v['Paused'] or v['Editing'] or v['Flight']) and v['RateOne'] and v['SurfaceEnabled']
 submit=submits[loop];done=completions[submit[8]];present=presents[loop]
 assert marker[0]<submit[0]<done[0] and submit[0]<present[0]
 joins.append({'loop':loop,'markerSequence':marker[0],'submission':submit[8],'submitSequence':submit[0],'completionSequence':done[0],'presentSequence':present[0],'distance':distance,'radius':radius})
assert cost['coverage'][0]['step']=='StartMeasurement' and cost['coverage'][-1]['step']=='Complete'
assert (markers[-1][1]-markers[0][1])/freq>=20
assert samples[0]['Loop']==markers[0][2]+1 and samples[-1]['Loop']==markers[-1][2]
assert [s['Loop'] for s in samples]==list(range(samples[0]['Loop'],samples[-1]['Loop']+1))
for s in samples:
 # Completion preserves its originating submission loop; sequence interval owns
 # the producer cost, not that historical loop field.
 loop=s['Loop'];assert s['Events']==returns[loop][0]-entries[loop][0]+1 and s['Cycles']>0 and s['Ticks']>0
 assert loop in submits and submits[loop][8] in completions and loop in presents
us=[s['Ticks']*1e6/freq for s in samples];perEvent=[u/s['Events'] for u,s in zip(us,samples)]
measuredLoops={s['Loop'] for s in samples}
heldCadence=[(entries[s['Loop']][1]-entries[s['Loop']-1][1])*1000/freq for s in samples]
heldWall=[(returns[s['Loop']][1]-entries[s['Loop']][1])*1000/freq for s in samples]
buckets=collections.defaultdict(list);frameBuckets=collections.defaultdict(list)
for s,u,f in zip(samples,us,heldCadence):
 bucket=int((entries[s['Loop']][1]-entries[samples[0]['Loop']][1])/freq);buckets[bucket].append(u);frameBuckets[bucket].append(f)
recurring=[{'second':b,'frames':len(v),'producerMeanUs':statistics.mean(v),'cadenceMeanMs':statistics.mean(frameBuckets[b])} for b,v in buckets.items() if b<19]
assert quant(us)['p99']<111.11 and all(b['producerMeanUs']<111.11 and b['cadenceMeanMs']<16.667 for b in recurring)
logs='\n'.join(p.read_text(errors='replace') for p in (out/'application-log').glob('segment-*.log'))
gpuSamples=[float(s) for s in re.findall(r'GPU timings: total=([0-9.]+) ms',logs)]
gpuMean=re.findall(r'GPU timing averages: total=([0-9.]+) ms',logs)
assert gpuSamples and gpuMean
result={'judgment':'PASS_BENIGN_LIVE_PENDING_INDEPENDENT_REVIEW','launches':1,'relaunches':0,'route':'Production Earth focus once; 5 s verified warmup then >=20 s verified held view; normal shutdown',
 'records':len(events),'frames':len(frameOrder),'successfulSubmissions':counts['7:2'],'positiveCompletions':len(completed),'presentReturns':counts['10:2'],'resourceBirths':lastbirth,'finalOpenOperations':len(opens),'finalPendingSubmissions':len(pending),'finalLiveResources':len(resources),
 'generationPublicationTuples':sorted(generations),'eventCounts':dict(counts),'heldFrames':len(samples),'heldSeconds':(markers[-1][1]-markers[0][1])/freq,'routeMarkerGpuJoins':joins,
 'producerPerFrameUs':quant(us),'producerAmortizedPerEventUs':quant(perEvent),'producerThreadCycles':quant([s['Cycles'] for s in samples]),'producerOwnedTimedAllocations':0,'retainedCapacity':header,'recurringOneSecondWindows':recurring,
 'phaseWallLatencyMs':{str(p):quant(v) for p,v in phases.items()},'wholeRunFrameCadenceMs':quant(frames),'heldFrameCadenceMs':quant(heldCadence),'heldFrameWallMs':quant(heldWall),'heldFramesAbove16_667ms':sum(x>16.667 for x in heldCadence),
 'nativeFrameSpanSeconds':(frameStarts[-1]-frameStarts[0])/freq,'gpuSampledMs':quant(gpuSamples),'gpuWholeRunMeanMs':float(gpuMean[-1]),
 'durableSequence':seq,'checkpoint':checkpoint,'journalTailByteMatches':seq-checkpoint,'allLiveEventChecksumsValid':True,'allOperationAndResourceCorrelationsValid':True,
 'maxSampledProducedMinusDurable':max(x['produced']-x['durable'] for x in water),'monotonicWatermarks':all(b['produced']>=a['produced'] and b['durable']>=a['durable'] for a,b in zip(water,water[1:])),
 'cleanRecoveryComplete':True,'terminal':recovery['Terminal'],'persistence':performance,'persistenceOneCorePercent':performance['cpuMs']/performance['wallMs']*100,'persistenceWriteMiBPerSecond':performance['BytesWritten']/1048576/(performance['wallMs']/1000),
 'measurementLimits':['Producer scopes measure bookkeeping including QPC/cycle-query overhead; tiny unscoped dispatch/setup instructions excluded. QPC includes scheduling; cycles are separately retained.','Live allocation count is actual recorder-owned storage; independent global-new CPU fixture verifies hot-path bypass absence. Retained capacity is not OS working set.','GPU samples are existing production timestamps; no recorder-disabled live A/B or per-frame GPU timestamp population is claimed. No unrelated-frame subtraction.','Only the benign distant-Earth held route was qualified; no close-Earth, ordinary exploration, blackout cause, recontact or Player PASS.']}
worst=max(measuredLoops,key=lambda loop:returns[loop][1]-entries[loop][1]);details=[]
for loop in range(worst-2,worst+3):
 pair={};phase=collections.defaultdict(float)
 for e in events[entries[loop][0]-1:returns[loop][0]]:
  if e[5]==1:pair[e[4],e[7]]=e
  elif e[5]==2:phase[str(e[4])]+=(e[1]-pair[e[4],e[7]][1])*1000/freq
 s=next(s for s in samples if s['Loop']==loop)
 details.append({'loop':loop,'producerUs':s['Ticks']*1e6/freq,'producerCycles':s['Cycles'],'phaseMs':dict(phase)})
write('outlier-analysis.json',details)
write('analysis.json',result)
write('retained-hashes.json',{str(p.relative_to(out)):{'bytes':p.stat().st_size,'sha256':sha(p)} for p in out.rglob('*') if p.is_file() and p.name!='retained-hashes.json'})
print(json.dumps({k:v for k,v in result.items() if k not in ('eventCounts','phaseWallLatencyMs','routeMarkerGpuJoins','recurringOneSecondWindows','measurementLimits')},indent=2))
