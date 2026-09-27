"""Read-only full, unrotated qualification journal cross-check; no recovery writes."""
import argparse,collections,hashlib,importlib.util,json,pathlib,struct
p=argparse.ArgumentParser();p.add_argument('session',type=pathlib.Path);p.add_argument('output',type=pathlib.Path);p.add_argument('--flight-report',type=pathlib.Path);a=p.parse_args()
assert not a.output.resolve().is_relative_to(a.session.resolve())
meta=json.loads((a.session/'session.json').read_text());recovery=json.loads((a.session/'recovery.json').read_text());target=recovery['DurableSequence']
u=lambda b,o:struct.unpack_from('<Q',b,o)[0]
records={};unused=[]
for name in ('bank0.bin','bank1.bin'):
    b=(a.session/name).read_bytes()
    if not any(b[:4096]):
        assert not any(b), 'Uninitialized bank has nonzero payload'
        unused.append(name);continue
    assert hashlib.sha256(b[:4064]).digest()==b[4064:4096]
    incarnation=u(b,24)
    for offset in range(4096+32*1024*1024,len(b),4096):
        page=b[offset:offset+4096]
        if u(page,16)!=incarnation or not 1<=u(page,32)<=15 or hashlib.sha256(page[:224]).digest()!=page[224:256]:continue
        for i in range(u(page,32)):
            e=page[256+i*256:512+i*256];w=struct.unpack('<32Q',e);h=14695981039346656037
            for x in e[:240]:h=((h^x)*1099511628211)&((1<<64)-1)
            assert w[0]==w[31]==u(page,24)+i and h==w[30]
            if w[0]<=target:
                assert w[0] not in records or records[w[0]]==w
                records[w[0]]=w
assert sorted(records)==list(range(1,target+1)), 'This full-span summary cannot be used for rotated suffix-only evidence'
events=[records[n] for n in sorted(records)]
counts=collections.Counter((e[4],e[5]) for e in events)
present=[e for e in events if e[4]==10 and e[5]==2 and e[6]==0]
submits=[e[8] for e in events if e[4]==7 and e[5]==2 and e[6]==0]
completed=[e[8] for e in events if e[4]==9]
spec=importlib.util.spec_from_file_location('stats',pathlib.Path(__file__).with_name('analyze-post-contact.py'));module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
cadences=[(b[1]-a[1])*1000/meta['qpcFrequency'] for a,b in zip(present,present[1:])]
result={'session':meta['session'],'recordCount':len(events),'sequenceRange':[1,target],'unusedBanks':unused,
        'fullSpanChecksumsValid':True,'complete':recovery['Complete'],
        'phaseEdgeCounts':{f'{phase}/{edge}':v for (phase,edge),v in sorted(counts.items())},
        'resourceInfoActionCounts':dict(collections.Counter(e[17] for e in events if e[4]==12 and e[5]==0)),
        'successfulSubmits':len(submits),'completedEvents':len(completed),
        'submitCompletionIdentityEquality':sorted(submits)==sorted(completed),
        'successfulPresents':len(present),'presentReturnCadenceMs':module.stats(cadences),
        'resourceBirthRetirementIdentityEquality':sorted((e[18],e[13],e[14]) for e in events if e[4]==12 and e[5]==0 and e[17]==1)==sorted((e[18],e[13],e[14]) for e in events if e[4]==12 and e[5]==0 and e[17]==2),
        'presentMeaning':'CPU API return cadence, not physical monitor scanout',
        'files':{f.name:{'bytes':f.stat().st_size,'sha256':hashlib.sha256(f.read_bytes()).hexdigest()} for f in a.session.iterdir() if f.is_file()}}
if a.flight_report:
    ids={r['DisplayFrame'] for r in json.loads(a.flight_report.read_text())['rows']}
    pairs=[(b[2],(b[1]-a[1])*1000/meta['qpcFrequency']) for a,b in zip(present,present[1:]) if b[2] in ids]
    assert {f for f,_ in pairs}==ids
    result['flightPresentReturnCadenceMs']=module.stats((v for _,v in pairs),frame_ids=(f for f,_ in pairs))
a.output.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k not in ('files','phaseEdgeCounts')},indent=2))
assert result['submitCompletionIdentityEquality']
assert result['resourceBirthRetirementIdentityEquality']
