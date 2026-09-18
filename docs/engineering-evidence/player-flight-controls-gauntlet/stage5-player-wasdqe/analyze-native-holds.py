from pathlib import Path
import json,hashlib
r=Path.cwd();e=r/'docs/engineering-evidence/player-flight-controls-gauntlet/stage5-player-wasdqe';b=r/'build/player-flight-controls-gauntlet/native-hold-observer'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def load(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def native(p):
    text=(p/'stdout.txt').read_text();assert 'overflow=0' in text and 'overflow=1' not in text
    return [dict(zip(['tick','frame','kind','message','key','before','after','active'],map(int,x.split()[1:]))) for x in text.splitlines() if x.startswith('HOLD_NATIVE ')]
def demand(bits):return [int(bool(bits&1))-int(bool(bits&2)),int(bool(bits&4))-int(bool(bits&8)),int(bool(bits&16))-int(bool(bits&32))]
bits={'W':1,'S':2,'A':4,'D':8,'Q':16,'E':32}
def mask(keys):return sum(bits[c] for c in keys)
tap=b/'tap';n=native(tap);m=load(tap/'snapshots.json');assert not m['overflow'];taps=[]
for key in 'WQ':
    events=[x for x in n if x['kind']==1 and x['key']==ord(key) and x['message'] in (256,257)];assert len(events)==2
    down,up=events;assert down['after']==bits[key] and up['after']==0 and down['frame']==up['frame']
    assert not any(x['Keys']&bits[key] for x in m['rows'])
    taps.append(dict(key=key,down=down,up=up,interveningSnapshots=0,managedHeldSnapshots=0))
out=[]
names=['W','S','A','D','Q','E','off-WA','off-WQ','off-AQ','off-WAQ','opposed-WS','opposed-AD','opposed-QE','focus-W-retry1','focus-WAQ','celestial-earth','celestial-moon']
for name in names:
    p=b/name;driver=load(p/'driver.json');assert driver['error'] is None and driver['exit']==0
    scenario=driver['scenario'];m=load(p/'snapshots.json');assert not m['overflow'];rows=m['rows'];n=native(p);frames=[x for x in n if x['kind']==2];assert len(frames)==len(rows)
    for a,z in zip(frames,rows):assert (a['frame'],a['after'],a['active'],a['key'])==(z['Frame'],z['Keys'],z['Active'],z['Engine'])
    assert len(set((x['Vessel'],x['Generation']) for x in rows))==1
    for x in rows:
        if x['Frontier']<128:assert [x['Pitch'],x['Yaw'],x['Roll']]==demand(x['Keys'] if x['Active'] else 0)
    save=load(p/'session.json');admissions=save['live']['admissions'];assert save['current']['frontier']==128
    transitions=[]
    for i,x in enumerate(rows):
        if i==0 or (x['Keys'],x['Sequence'])!=(rows[i-1]['Keys'],rows[i-1]['Sequence']):
            transitions.append({k:x[k] for k in ['Tick','Frame','Keys','Active','Frontier','Sequence','Pitch','Yaw','Roll','Main','ViewKind','ViewBody']})
    for a in admissions:
        observed=next(x for x in rows if x['Sequence']==a['sequence']);assert a['frontier']<=observed['Frontier']<=a['frontier']+4
    held=[]
    for key in bits:
        events=[x for x in n if x['kind']==1 and x['key']==ord(key) and x['message'] in (256,257)]
        for i,down in enumerate(events):
            if down['message']!=256:continue
            up=next((x for x in events[i+1:] if x['message']==257),None);assert up is not None
            fs=[x for x in frames if down['frame']<x['frame']<=up['frame'] and x['after']&bits[key]]
            assert len(fs)>=2
            released=next((x for x in frames if x['frame']>up['frame'] and not x['after']&bits[key]),None);assert released is not None
            held.append(dict(key=key,down=down,firstHeld=fs[0],heldSnapshots=len(fs),up=up,firstReleased=released))
    expectedKeys=scenario[4:] if scenario.startswith('off-') else scenario[8:] if scenario.startswith('opposed-') else scenario[6:] if scenario.startswith('focus-') else ('W' if scenario=='celestial-earth' else 'WAQ') if scenario.startswith('celestial-') else scenario
    chord=mask(expectedKeys);chordRows=[x for x in rows if x['Keys']==chord and x['Frontier']<128];assert len(chordRows)>=2
    if scenario.startswith('opposed-'):
        assert all([x['Pitch'],x['Yaw'],x['Roll']]==[0,0,0] for x in chordRows)
        upSecond=next(x for x in held if x['key']==expectedKeys[1])['up'];remaining=[x for x in rows if x['Frame']>upSecond['frame'] and x['Keys']==bits[expectedKeys[0]]];assert len(remaining)>=2
    focus=None
    if scenario.startswith('focus-'):
        loss=next(x for x in driver['events'] if x.get('label')=='sink');returned=next(x for x in driver['events'] if x.get('label')=='candidate-return-old-keys-down');fresh=[x for x in driver['events'] if x['kind']=='down' and x.get('key')==expectedKeys[0]][1]
        neutral=[x for x in rows if loss['tick']+30<x['Tick']<fresh['tick']];assert len(neutral)>=2 and all(x['Keys']==0 and [x['Pitch'],x['Yaw'],x['Roll']]==[0,0,0] and x['Main'] for x in neutral)
        returnedRows=[x for x in neutral if x['Tick']>returned['tick']+30];assert len(returnedRows)>=2 and any(x['Active']==1 for x in returnedRows)
        assert any(x['message']==8 for x in n if x['kind']==1)
        focus=dict(loss=loss,returned=returned,fresh=fresh,neutralSnapshots=len(neutral),returnedNeutralSnapshots=len(returnedRows),engineLatchPreserved=True)
    if scenario.startswith('celestial-'):
        assert all(x['ViewKind']==0 for x in chordRows)
        assert any(x['ViewKind']==2 and x['Tick']>chordRows[-1]['Tick'] for x in rows)
    terminal=[x for x in rows if x['Frontier']==128];assert len(terminal)>=2 and any(x['Engine']==1 for x in terminal)
    assert len(set(x['Sequence'] for x in terminal))==1
    ready=[x for x in rows if x['Sequence']==0];assert len(ready)>=2 and all(x['Frontier']==0 for x in ready)
    out.append(dict(case=name,scenario=scenario,status='NATIVE PASS; physical oracle separate',snapshotCount=len(rows),held=held,chordSnapshots=len(chordRows),transitions=transitions,admissions=admissions,focus=focus,stableIdentity=[rows[0]['Vessel'],rows[0]['Generation']],terminalRefusal=True,files=[dict(path=str((p/f).relative_to(r)),sha256=sha(p/f)) for f in ['driver.json','session.json','snapshots.json','stdout.txt','stderr.txt']]))
(e/'native-hold-results.json').write_text(json.dumps(dict(cause='HARNESS / EVENT-DURATION MISMATCH in repeated original Sky mechanism',taps=taps,results=out),indent=2)+'\n')
(e/'focus-loss.json').write_text(json.dumps([dict(case=x['case'],focus=x['focus']) for x in out if x['focus']],indent=2)+'\n')
print(json.dumps(dict(tapCause='PROVEN',nativeCases=len(out),singleAxis=6,opposing=3,simultaneous=4,focus=2,celestial=2)))
