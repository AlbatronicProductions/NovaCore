"""Read-only independent inspection of original S24 exports (legacy V1 and V2)."""
import argparse
import base64
import hashlib
import json
import math
from pathlib import Path
import re
import struct
import uuid

MAX_BYTES=8*1024*1024
MASK=(1<<64)-1

def unique(pairs):
    result={}
    for key,value in pairs:
        if key in result:raise ValueError('Duplicate JSON property: '+key)
        result[key]=value
    return result

def strict_json(raw):
    return json.loads(raw,object_pairs_hook=unique,parse_constant=lambda value:(_ for _ in ()).throw(ValueError('Nonfinite JSON '+value)))

def compact(value):return json.dumps(value,sort_keys=True,separators=(',',':'),ensure_ascii=False)
def require(condition,message):
    if not condition:raise ValueError(message)
def number(value):return type(value) in (int,float) and math.isfinite(value) and value>=0
def integer(value):return type(value) is int and value>=0
def identifier(value):
    try:return isinstance(value,str) and str(uuid.UUID(value))==value.lower()
    except (ValueError,AttributeError):return False

def decimal(value):return isinstance(value,str) and re.fullmatch(r'0|[1-9][0-9]{0,19}',value) is not None and int(value)<=MASK
def hexid(value):return isinstance(value,str) and re.fullmatch('[a-f0-9]{32}',value) is not None
def binding(o):
    require(isinstance(o,dict) and identifier(o.get('session')),'Recorder session invalid')
    for role in ('producer','observer'):
        require(integer(o.get(role+'Pid')) and o[role+'Pid']>0 and decimal(o.get(role+'StartUtcTicks')),'Recorder process incarnation invalid')
    require(type(o.get('fixture')) is bool,'Fixture identity invalid')
    return [o['session'],o['producerPid'],o['producerStartUtcTicks'],o['observerPid'],o['observerStartUtcTicks'],o['fixture']]

def beacon_binding(o):
    require(isinstance(o,dict) and identifier(o.get('witness')) and identifier(o.get('probeId')) and integer(o.get('pid')) and o['pid']>0 and decimal(o.get('startUtcTicks')),'CPU incarnation invalid')
    return [o['witness'],o['probeId'],o['pid'],o['startUtcTicks']]

def event_check(o):
    if not o.get('eventValid'):return
    raw=base64.b64decode(o['latestEventBase64'],validate=True);require(len(raw)==256,'Raw event length')
    words=struct.unpack('<32Q',raw);h=14695981039346656037
    for byte in raw[:240]:h=((h^byte)*1099511628211)&MASK
    require(words[0]==words[31]==int(o['producedBefore']) and words[30]==h,'Raw event checksum/commit invalid')
    require(o.get('latestEventWords')==[str(w) for w in words[:30]],'Raw event words mismatch / lost integer precision')

def inspect_bytes(raw,expected_witness=None):
    require(len(raw)<=MAX_BYTES,'Export exceeds 8 MiB');v=strict_json(raw)
    require(isinstance(v,dict) and v.get('schema') in ('NovaCore.PhoneWitness/1','NovaCore.PhoneWitness/2'),'Unknown phone export schema')
    version=int(v['schema'][-1]);witness=v.get('witness');require(identifier(witness),'Witness UUID invalid')
    require(not expected_witness or witness==expected_witness,'Wrong expected witness incarnation')
    require(isinstance(v.get('rows'),list) and len(v['rows'])<=2048,'Invalid row list')
    require(isinstance(v.get('marks'),list) and len(v['marks'])<=16,'Invalid marker list')
    require(isinstance(v.get('preserved'),dict),'Invalid preserved windows')
    require(integer(v.get('dropped')),'Invalid rollover counter')
    # Pretty-printed download may be larger than the bounded compact localStorage payload.
    compact_chars=len(compact(v));require(compact_chars<=768*1024+4096,'Capture compact budget exceeded')
    errors=[];limitations=[];all_rows={};nonces={};marks=[];proofs=[];windows={}
    def attempt(label,call):
        try:call()
        except (ValueError,TypeError,KeyError,IndexError,OverflowError) as error:errors.append(label+': '+str(error))
    if version==2:
        require(hexid(v.get('pageIncarnation')) and integer(v.get('nextSequence')),'V2 page/sequence invalid')
        require(isinstance(v.get('receiverBuild'),str) and re.fullmatch('[a-f0-9]{64}',v['receiverBuild']) is not None,'V2 receiver build invalid')
        expected_binding=strict_json(v['sessionBinding']) if v.get('sessionBinding') is not None else None
        expected_beacon=beacon_binding(v.get('beaconIdentity'));require(expected_beacon[0]==witness,'Foreign capture beacon')
    else:
        limitations.append('Legacy V1 omits persisted send-side nonce, marker generation and exact process incarnation binding')
        if not v.get('pageIncarnation'):limitations.append('Legacy V1 has no page incarnation; same-page post-marker proof cannot be established')
    def check_row(row):
        require(isinstance(row,dict) and isinstance(row.get('kind'),str),'Row shape')
        kind=row['kind']
        if version==2:
            require(row.get('pageIncarnation')==v['pageIncarnation'] and integer(row.get('sequence')) and 0<row['sequence']<v['nextSequence'],'Foreign page/sequence')
            row_id=row['sequence']
        else:row_id=compact(row)
        if row_id in all_rows:require(compact(all_rows[row_id])==compact(row),'Conflicting row sequence');return
        all_rows[row_id]=row
        if kind not in ('response','request-failure'):
            require(number(row.get('phoneMono')) and number(row.get('phoneWall')),'Page event clocks');return
        for key in ('sendMono','sendWall','receiveMono','receiveWall'):require(number(row.get(key)),'Request clock '+key)
        require(row['receiveMono']>=row['sendMono'],'Reversed request interval')
        if version==2:
            require(row.get('source') in ('primary','beacon') and isinstance(row.get('nonce'),str) and re.fullmatch('[a-zA-Z0-9]{16,96}',row['nonce']) is not None,'Request source/nonce')
            require(integer(row.get('generation')) and (row.get('markerId') is None or hexid(row['markerId'])),'Request generation/marker')
        if kind=='request-failure':return
        data=row.get('data');require(isinstance(data,dict) and data.get('witness')==witness,'Foreign/malformed response')
        for key in ('serverMonotonicNs','serverUtcNs'):require(decimal(data.get(key)),'Server clock '+key)
        nonce=data.get('nonce');require(isinstance(nonce,str) and nonce.isascii() and nonce.isalnum() and 1<=len(nonce)<=96,'Response nonce')
        nk=(row.get('source','primary'),nonce);require(nk not in nonces,'Nonce reused across different rows');nonces[nk]=row_id
        if version==2:
            require(row['nonce']==nonce and type(row.get('accepted')) is bool,'Request/response nonce or admission mismatch')
            if row['source']=='beacon':
                require(data.get('schema')=='NovaCore.CpuBeacon/1' and beacon_binding(data)==expected_beacon,'Foreign CPU response');return
            require(data.get('receiverBuild')==v['receiverBuild'] and beacon_binding(data.get('beacon'))==expected_beacon,'Primary build/CPU binding mismatch')
        require(data.get('schema')=='NovaCore.BlackoutWitness/1','Primary schema')
        require(integer(data.get('localCommittedSerial')) and integer(data.get('localSkippedSamples')),'Writer counters')
        require(data.get('diskError') is None or isinstance(data['diskError'],str),'Writer fault field')
        s=data.get('sample')
        if s is None:
            require(version==2 and (data.get('mode')=='recovery' or data['localCommittedSerial']==0),'Missing sample');return
        require(isinstance(s,dict) and s.get('schema')=='NovaCore.BlackoutWitness/1' and s.get('witness')==witness and integer(s.get('serial')) and s['serial']>0,'Sample identity/serial')
        require(decimal(s.get('sampleMonotonicNs')) and decimal(s.get('utcNs')),'Sample clocks')
        o=s.get('observation');require(isinstance(o,dict),'Observation shape')
        if o.get('status'):
            require(o['status'] in ('waiting-for-new-session','sample-error'),'Unknown sample status');return
        require(identifier(o.get('session')),'Observation session')
        for field in ('producedBefore','producedAfter','durable','observerHeartbeat','producerHeartbeat'):require(decimal(o.get(field)),'Exact word '+field)
        if version==2:
            require(binding(o)==expected_binding,'Foreign recorder incarnation')
            for field in ('headerProduced','consumed','dropped','producerFault','observerFault','done','qpc'):require(decimal(o.get(field)),'Exact word '+field)
            require(decimal(o.get('ready')) or o.get('ready')=='-1','Ready state')
            require(integer(o.get('qpcFrequency')) and o['qpcFrequency']>0,'QPC frequency')
            for field in ('producerProcess','observerProcess'):require(o.get(field) in ('alive','exited','query-failed'),'Process state')
            for field in ('eventValid','headerWithinBracket','stableProduced'):require(type(o.get(field)) is bool,'Observation flag')
            event_check(o)
    for i,row in enumerate(v['rows']):attempt(f'rows[{i}]',lambda row=row:check_row(row))
    for name,window in v['preserved'].items():
        def check_window():
            require(name in ('firstFailure','blackout') and isinstance(window,dict) and isinstance(window.get('prelude'),list) and len(window['prelude'])<=120,'Window shape')
            require(len(compact(window['prelude']))<=(96 if version==2 else 128)*1024,'Window character bound')
            for row in window['prelude']:check_row(row)
            if 'failure' in window:check_row(window['failure']);require(window['failure']['kind']=='request-failure','First failure row kind')
            windows[name]=dict(rows=len(window['prelude']),compactCharacters=len(compact(window['prelude'])))
        attempt('preserved.'+name,check_window)
    mark_ids=set();generations=set()
    for i,mark in enumerate(v['marks']):
        def check_mark():
            require(isinstance(mark,dict) and mark.get('witness')==witness and number(mark.get('phoneMono')) and number(mark.get('phoneWall')),'Marker identity/timing')
            marks.append(mark)
            if version==1:return
            require(mark.get('pageIncarnation')==v['pageIncarnation'] and hexid(mark.get('id')) and integer(mark.get('generation')) and mark['generation']>0,'Marker page/generation')
            require(mark['id'] not in mark_ids and mark['generation'] not in generations,'Repeated marker identity');mark_ids.add(mark['id']);generations.add(mark['generation'])
            require(mark.get('binding') is None or strict_json(mark['binding'])==expected_binding,'Marker session binding')
            require(isinstance(mark.get('proofs'),dict),'Marker proofs')
            for source,row in mark['proofs'].items():
                require(source in ('primary','beacon'),'Unknown proof source');check_row(row)
                require(row['kind']=='response' and row['source']==source and row['accepted'] is True and row['visible']=='visible','Proof response not admitted')
                require(row['markerId']==mark['id'] and row['generation']==mark['generation'] and row['sendMono']>mark['phoneMono'] and row['receiveMono']-row['sendMono']<=1800,'Proof is not a fresh post-marker request')
                proofs.append(dict(marker=i,source=source,nonce=row['nonce'],sendAfterMarkerMs=row['sendMono']-mark['phoneMono'],rttMs=row['receiveMono']-row['sendMono'],sequence=row['sequence']))
        attempt(f'marks[{i}]',check_mark)
    rows=list(all_rows.values());responses=[r for r in rows if r.get('kind')=='response'];primary=[r for r in responses if r.get('data',{}).get('schema')=='NovaCore.BlackoutWitness/1' and isinstance(r['data'].get('sample'),dict)]
    responses.sort(key=lambda r:r.get('sendMono',0));primary.sort(key=lambda r:r.get('sendMono',0))
    failures=[r for r in rows if r.get('kind')=='request-failure']
    marker_summary=[]
    for mark in marks:
        earlier=[r for r in responses if number(r.get('receiveMono')) and r['receiveMono']<=mark['phoneMono']]
        marker_summary.append(dict(phoneMono=mark['phoneMono'],phoneWall=mark['phoneWall'],lastReplyToMarkerMs=mark['phoneMono']-earlier[-1]['receiveMono'] if earlier else None,requestsSentAfter=sum(r['sendMono']>mark['phoneMono'] for r in responses),failuresSentAfter=sum(r['sendMono']>mark['phoneMono'] for r in failures)))
    return dict(inputSha256=hashlib.sha256(raw).hexdigest(),bytes=len(raw),compactCharacters=compact_chars,schema=v['schema'],witness=witness,structuralPass=not errors,errors=errors,limitations=limitations,rollingRows=len(v['rows']),uniqueRowsIncludingPinned=len(rows),uniqueResponsesIncludingPinned=len(responses),uniqueFailuresIncludingPinned=len(failures),marks=len(marks),markerDetails=marker_summary,preservedWindows=windows,dropped=v['dropped'],storageReady=v.get('storageReady'),storageError=v.get('storageError'),userAgent=v.get('userAgent'),firstSerial=primary[0]['data']['sample']['serial'] if primary else None,lastSerial=primary[-1]['data']['sample']['serial'] if primary else None,fixtures=sorted({r['data']['sample']['observation'].get('fixture') is True for r in primary}),maxReplyMs=max((r['receiveMono']-r['sendMono'] for r in responses),default=None),postMarkerProofs=proofs,qualification='Original artifact inspection; this is neither source authentication nor physical blackout proof. Missing historical fields are not invented.')

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('input',type=Path);p.add_argument('--expected-witness');p.add_argument('--output',type=Path);a=p.parse_args()
    raw=a.input.read_bytes()
    try:result=inspect_bytes(raw,a.expected_witness)
    except (ValueError,TypeError,KeyError,IndexError,RecursionError) as error:result=dict(inputSha256=hashlib.sha256(raw).hexdigest(),structuralPass=False,errors=[str(error)])
    text=json.dumps(result,indent=2)
    if a.output:
        with a.output.open('x',encoding='utf-8') as stream:stream.write(text+'\n')
    print(text);return 0 if result['structuralPass'] else 1
if __name__=='__main__':raise SystemExit(main())
