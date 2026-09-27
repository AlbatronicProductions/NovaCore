"""Read-only bounded-journal forensics. Never writes into the input session."""
import argparse, bisect, collections, hashlib, json, pathlib, struct
p=argparse.ArgumentParser();p.add_argument('session',type=pathlib.Path);p.add_argument('output',type=pathlib.Path);a=p.parse_args()
assert not a.output.resolve().is_relative_to(a.session.resolve())
def u(b,o):return struct.unpack_from('<Q',b,o)[0]
def sha(b):return hashlib.sha256(b).digest()
def valid(b):
    if len(b)!=256 or u(b,0)!=u(b,248):return False
    h=14695981039346656037
    for x in b[:240]:h=((h^x)*1099511628211)&((1<<64)-1)
    return u(b,240)==h
session=json.loads((a.session/'session.json').read_text());recovery=json.loads((a.session/'recovery.json').read_text())
records={};pages=[];banks=[]
for name in ('bank0.bin','bank1.bin'):
    b=(a.session/name).read_bytes();assert sha(b[:4064])==b[4064:4096]
    incarnation=u(b,24);bank={'name':name,'incarnation':incarnation,'checkpoint':u(b,32),'checkpointBytes':u(b,40)};banks.append(bank)
    for offset in range(4096+32*1024*1024,len(b),4096):
        page=b[offset:offset+4096]
        if u(page,16)!=incarnation or not 1<=u(page,32)<=15 or sha(page[:224])!=page[224:256]:continue
        count=u(page,32);seq=u(page,24);entries=[page[256+i*256:512+i*256] for i in range(count)]
        assert all(valid(e) and u(e,0)==seq+i for i,e in enumerate(entries))
        pages.append({'start':seq,'count':count,'produced':u(page,40),'faults':u(page,48),'dropped':u(page,56),'observedBacklogBeforeCopy':u(page,40)-seq+1})
        for e in entries:records[u(e,0)]=struct.unpack('<32Q',e)
events=sorted(records.values());qpc=sorted(e[1] for e in events);freq=session['qpcFrequency'];frames=collections.Counter(e[2] for e in events if e[2])
windows={}
for ms in [.01,.1,1,10,100,1000]:
    ticks=ms*freq/1000
    maximum=max(i-bisect.bisect_left(qpc,t-ticks)+1 for i,t in enumerate(qpc))
    windows[str(ms)]={'events':maximum,'eventsPerSecond':maximum*1000/ms}
open_events=[e['Words'] for e in recovery['Open'].values()];pending=[e['Words'] for e in recovery['Pending'].values()]
timed=open_events+pending
result={'limits':'Current bank incarnations retain only a recent suffix; checkpoints preserve older unmatched witnesses, not a full historical trace. Peaks below are retained-suffix lower bounds for the whole run. Exact first overflow, historical queue trajectory and disk stall timing were not recorded.',
 'files':{f.name:{'bytes':f.stat().st_size,'sha256':hashlib.sha256(f.read_bytes()).hexdigest()} for f in a.session.iterdir() if f.is_file()},
 'banks':banks,'retainedRecords':len(events),'sequenceRange':[events[0][0],events[-1][0]],'timeRangeAfterOpenSeconds':[(min(qpc)-session['openedQpc'])/freq,(max(qpc)-session['openedQpc'])/freq],
 'peakSlidingWindowsMs':windows,'eventsPerFrameHistogram':dict(sorted(collections.Counter(frames.values()).items())), 'eventClasses':dict(sorted(collections.Counter(e[4] for e in events).items())),
 'pages':len(pages),'maximumObservedPreCopyBacklog':max(p['observedBacklogBeforeCopy'] for p in pages),'pageOccupancyHistogram':dict(sorted(collections.Counter(p['count'] for p in pages).items())),
 'unmatched':{'open':len(open_events),'pending':len(pending),'faultWords':dict(collections.Counter(e[29] for e in timed)),'openPhases':dict(collections.Counter(e[4] for e in open_events)), 'timeRangeAfterOpenSeconds':[(min(e[1] for e in timed)-session['openedQpc'])/freq,(max(e[1] for e in timed)-session['openedQpc'])/freq],'frameRange':[min(e[2] for e in timed),max(e[2] for e in timed)]},
 'persistence':json.loads((a.session/'performance.json').read_text())}
a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k not in ('files','persistence')},indent=2))
