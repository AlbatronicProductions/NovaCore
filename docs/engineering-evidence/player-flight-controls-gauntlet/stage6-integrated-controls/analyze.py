from pathlib import Path
import json,hashlib,re
r=Path.cwd();b=r/'build/player-flight-controls-gauntlet/native-hold-observer-stage6';e=r/'docs/engineering-evidence/player-flight-controls-gauntlet/stage6-integrated-controls'
def load(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
out=[]
for p in sorted(b.glob('run-*')):
 d=load(p/'driver.json');assert d['exit']==0 and d['error'] is None
 m=load(p/'snapshots.json');assert not m['overflow'];rows=m['rows'];t=(p/'stdout.txt').read_text();assert 'overflow=0' in t and 'overflow=1' not in t
 n=[list(map(int,x.split()[1:])) for x in t.splitlines() if x.startswith('HOLD_NATIVE ')];frames=[x for x in n if x[2]==2];assert len(frames)==len(rows)
 for a,z in zip(frames,rows):assert (a[1],a[6],a[7],a[4])==(z['Frame'],z['Keys'],z['Active'],z['Engine'])
 assert set((x['Vessel'],x['Generation']) for x in rows)=={(201,1)}
 for x in rows:
  k=x['Keys'] if x['Active'] else 0
  if x['Frontier']<128:assert [x['Pitch'],x['Yaw'],x['Roll']]==[int(bool(k&1))-int(bool(k&2)),int(bool(k&4))-int(bool(k&8)),int(bool(k&16))-int(bool(k&32))]
 for keys,main in [(21,True),(42,False),(8,False)]:assert len([x for x in rows if x['Keys']==keys and x['Main']==main and x['Frontier']<128])>=2
 held=[]
 for key,bit in zip('WSADQE',[1,2,4,8,16,32]):
  ev=[x for x in n if x[2]==1 and x[4]==ord(key) and x[3] in [256,257]]
  for i,down in enumerate(ev):
   if down[3]!=256:continue
   up=next(x for x in ev[i+1:] if x[3]==257);hs=[x for x in rows if down[1]<x['Frame']<=up[1] and x['Keys']&bit];assert len(hs)>=2
   released=next(x for x in rows if x['Frame']>up[1] and not x['Keys']&bit)
   held.append(dict(key=key,downTick=down[0],downFrame=down[1],upTick=up[0],upFrame=up[1],heldSnapshots=len(hs),firstHeldFrame=hs[0]['Frame'],firstReleasedFrame=released['Frame']))
 drag=[x for x in rows if x['Look'] and (x['MouseX'] or x['MouseY'])];assert len(drag)>=2
 assert sum(x['MouseX'] for x in drag)==48 and sum(x['MouseY'] for x in drag)==-24
 for x in drag:
  prior=rows[rows.index(x)-1];assert prior['Look'] and x['CameraOrientation']!=prior['CameraOrientation'] and x['ViewKind']==2
 wheel=next(x for x in rows if x['Wheel']);prior=rows[rows.index(wheel)-1];assert abs(wheel['OrbitDistance']/prior['OrbitDistance']-.8)<1e-12
 earth=next(x for x in rows if x['Focus']==4);assert earth['ViewKind']==0 and earth['ViewBody']==6
 assert len([x for x in rows if x['Keys']==8 and x['ViewKind']==0 and x['ViewBody']==6])>=2
 f=next(x for x in rows if x['CameraAction']==1);assert f['ViewKind']==2 and f['OrbitDistance']==wheel['OrbitDistance']
 ready=[x for x in rows if x['Sequence']==0];assert len(ready)>=2 and all(x['Frontier']==0 for x in ready)
 terminal=[x for x in rows if x['Frontier']==128];assert any(x['Engine']==1 for x in terminal) and len(set(x['Sequence'] for x in terminal))==1
 s=load(p/'session.json');ads=s['live']['admissions'];assert s['live']['capacity']==256 and s['live']['execution']=='PhysicalActuators' and len(ads)==8
 assert all(a['sequence']==i+1 and a['identity']==dict(vessel=dict(value=201),generation=1) for i,a in enumerate(ads))
 compact=[dict(frontier=a['frontier'],sequence=a['sequence'],requested=a['requested'],effective=a['effective']) for a in ads]
 out.append(dict(run=p.name,status='PASS',snapshots=len(rows),held=held,admissions=compact,camera=dict(dragFrames=[x['Frame'] for x in drag],delta=[48,-24],zoomBefore=prior['OrbitDistance'],zoomAfter=wheel['OrbitDistance'],earthFrame=earth['Frame'],refocusFrame=f['Frame']),timing=[x for x in t.splitlines() if x.startswith(('STOCK_ASSEMBLY_FRAME ','SRV01_LIVE_SERVICE '))],files=[dict(path=str((p/z).relative_to(r)),sha256=sha(p/z)) for z in ['driver.json','snapshots.json','session.json','stdout.txt','stderr.txt']]))
assert len(out)==5
(e/'native-results.json').write_text(json.dumps(dict(status='PASS',runs=out),indent=2)+'\n')
print('Integrated native transport/camera PASS 5/5')
