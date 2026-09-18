"""Bounded lifecycle analysis; timestamps, not perception or causal assumptions."""
import json,pathlib,math,hashlib,re
p=pathlib.Path(__file__).resolve().parent
scratch=p.parents[4]/'build/srv01-startup-first-present'
order=['A1','B1','B2','A2']
codes={1:'before_ShowWindow',2:'ShowWindow_return',3:'window_created',4:'native_window_initialization_entry',5:'instance_ready',6:'device_ready',7:'visual_meshes_ready',8:'initial_swap_ready',9:'backend_ready',10:'native_frame_begin',11:'native_frame_end',12:'graphics_fence_return',13:'first_post_successful_present_callback',14:'callback_begin',15:'callback_end',16:'acquire_return',17:'present_begin',18:'present_return',19:'recreate_begin'}
out=[]
def stats(v):
 s=sorted(v)
 return {k:s[max(0,math.ceil(q*len(s))-1)] for k,q in [('median',.5),('p95',.95),('p99',.99),('max',1)]}
for ident in order:
 path=scratch/f'{ident}.process.json'
 if not path.exists():out.append({'id':ident,'result':'NOT EXECUTED'});continue
 a=json.loads(path.read_text(encoding='utf-8-sig'));out.append(a);a['result']='INVALID'
 log=scratch/f'{ident}.stdout.txt';text=log.read_text(encoding='utf-8-sig',errors='replace');a['stdoutSha256']=hashlib.sha256(log.read_bytes()).hexdigest()
 def report(prefix):
  found=[line[len(prefix):] for line in text.splitlines() if line.startswith(prefix)]
  if len(found)!=1:raise ValueError('Missing/duplicate '+prefix)
  return json.loads(found[0])
 try:m=report('STARTUP_MANAGED ');n=report('STARTUP_NATIVE ');frames=report('STARTUP_FRAMES ')
 except ValueError as e:a['reason']=str(e);continue
 rows=m.pop('rows');ev=n['events'];f=m['frequency'];origin=m['managedEntry'];ms=lambda t:(t-origin)*1000/f
 a['simulation']=m;a['frequency']=f;a['recordedExtents']=re.findall(r'EXHAUST_PRESENTATION_STORAGE .*?extent=(\d+x\d+)',text)
 a['events']=[{'tick':t,'msFromManagedEntry':ms(t),'name':codes[c],'code':c,'frame':frame,'value':v} for t,c,frame,v in ev]
 a['firstCallbackRows']=rows[:12]
 def event(code,predicate=lambda e:True):return next((e for e in ev if e[1]==code and predicate(e)),None)
 problems=[]
 if a.get('exitCode')!=0:problems.append('process exit')
 if f!=n['frequency']:problems.append('clock frequency mismatch')
 if len(rows)!=180 or len(frames)!=180 or m['callbacks']!=180 or m['legacySamples']!=179:problems.append('capture count')
 if not a['recordedExtents'] or any(x!='960x540' for x in a['recordedExtents']):problems.append('dimension mismatch')
 if m['failed'] or not m['identityPass'] or m['replayMatches']!=180:problems.append('canonical identity/reference failure')
 if m['maxPublications']>4 or m['credits']-m['time']!=m['debt']:problems.append('accounting/budget')
 if m['revision']!=m['frontier'] or m['history']!=m['frontier'] or m['timelineRevision']!=0:problems.append('revision/history')
 advanced=next((i for i,r in enumerate(rows) if r[10]>r[9]),None)
 required={name:event(code) for name,code in [('T1',3),('shown',2),('T2',9),('T3',10),('T4',11),('T5',17),('T7',13)]}
 required['T6']=event(18,lambda e:e[3]==0)
 if advanced is None or any(e is None for e in required.values()):problems.append('required lifecycle boundary unavailable')
 if problems:a['problems']=problems;continue
 a['result']='VALID';r=rows[advanced]
 times={'T0':origin,**{k:e[0] for k,e in required.items()},'T8_serviceEntryBound':r[5],'T9_publicationObservedUpperBound':r[7],'legacyStart':rows[0][2],'firstLegacySample':rows[1][3],'firstManagedCallbackEntry':rows[0][0]}
 a['timeline']={k:{'tick':v,'msFromManagedEntry':ms(v)} for k,v in times.items()}
 a['deltasMs']={
  'windowCreatedToBackendReady':(times['T2']-times['T1'])*1000/f,
  'windowCreatedToFirstNativeFrame':(times['T3']-times['T1'])*1000/f,
  'shownToFirstSuccessfulPresentReturn':(times['T6']-times['shown'])*1000/f,
  'createdToFirstSuccessfulPresentReturn':(times['T6']-times['T1'])*1000/f,
  'firstPresentCall':(times['T6']-times['T5'])*1000/f,
  'firstNativeFrame':frames[0],
  'firstLegacyInterval':rows[1][4]*1000/f,
  'legacyStartToFirstPresentReturn':(times['T6']-times['legacyStart'])*1000/f,
  'presentReturnToFirstAdvancingService':(times['T8_serviceEntryBound']-times['T6'])*1000/f,
  'firstAdvancingService':(r[6]-r[5])*1000/f,
  'firstServiceCall':(rows[1][6]-rows[1][5])*1000/f}
 a['serviceCallsBeforeFirstSuccessfulPresent']=sum(bool(r[5]) and r[5]<times['T6'] for r in rows)
 a['publicationsObservedBeforeFirstSuccessfulPresent']=sum(r[10]-r[9] for r in rows if r[7] and r[7]<times['T6'])
 a['firstAdvancingCallback']=advanced
 a['firstAdvancingObservation']=dict(frontierBefore=r[9],frontierAfter=r[10],debtBefore=r[11],credit=r[12],debtAfter=r[13],time=r[10]*1_000_000//60)
 display=[r[4]*1000/f for r in rows if r[3]]
 a['legacyDisplay']=stats(display);a['nativeFrames']=stats(frames)
 a['nativeFirstFrames']=frames[:12]
 a['legacyMaximumEndingCallback']=max(range(1,len(rows)),key=lambda i:rows[i][4])
 a['nativeMaximumIndex']=max(range(len(frames)),key=frames.__getitem__)
 a['initialCanonical']={'frontier':rows[0][9],'debt':rows[0][11]}
 a['environment']=[l for l in text.splitlines() if any(s in l for s in ['[native] GPU:','Vulkan validation layer','Swapchain recreated','CPU timings:','SRV01_COLD_PREPARATION'])][:8]
(p/'captures.json').write_text(json.dumps({'order':order,'nativeCodes':codes,'managedRowColumns':['entry','afterTitle','start','legacySample','legacyElapsed','serviceIn','serviceOut','observed','exit','frontierBefore','frontierAfter','debtBefore','credit','debtAfter'],'captures':out},indent=2)+'\n',encoding='utf-8')
for a in out:print(a['id'],a['result'],a.get('deltasMs',a.get('reason',a.get('problems',''))))
