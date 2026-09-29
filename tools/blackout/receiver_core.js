/* Pure evidence rules. No DOM, network, timers, or storage side effects. */
(function(root,factory){if(typeof module==='object'&&module.exports)module.exports=factory();else root.WitnessCore=factory();})(globalThis,()=>{
  'use strict';
  const SCHEMA='NovaCore.PhoneWitness/2',BUDGET=768*1024,PRELUDE=96*1024,LIMIT=2048,FRESH_MS=3000,REPLY_MS=1800;
  const object=v=>v!==null&&typeof v==='object'&&!Array.isArray(v);
  const finite=v=>typeof v==='number'&&Number.isFinite(v)&&v>=0;
  const integer=v=>Number.isSafeInteger(v)&&v>=0;
  const hex=v=>typeof v==='string'&&/^[a-f0-9]{32}$/.test(v);
  const uuid=v=>typeof v==='string'&&/^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$/i.test(v);
  const decimal=v=>typeof v==='string'&&/^(0|[1-9][0-9]{0,19})$/.test(v)&&BigInt(v)<=18446744073709551615n;
  const signed=v=>decimal(v)||(typeof v==='string'&&/^-[1-9][0-9]{0,18}$/.test(v)&&BigInt(v)>=-9223372036854775808n);
  const nonce=v=>typeof v==='string'&&/^[a-zA-Z0-9]{16,96}$/.test(v);
  const copy=v=>JSON.parse(JSON.stringify(v));
  const need=(test,message)=>{if(!test)throw Error(message);};
  function identity(o){
    if(!object(o)||!uuid(o.session)||!integer(o.producerPid)||!integer(o.observerPid)||!decimal(o.producerStartUtcTicks)||!decimal(o.observerStartUtcTicks))return null;
    return JSON.stringify([o.session,o.producerPid,o.producerStartUtcTicks,o.observerPid,o.observerStartUtcTicks,o.fixture===true]);
  }
  function beaconIdentity(o){return object(o)&&uuid(o.witness)&&uuid(o.probeId)&&integer(o.pid)&&o.pid>0&&decimal(o.startUtcTicks)?JSON.stringify([o.witness,o.probeId,o.pid,o.startUtcTicks]):null;}
  function binding(value){
    try{const a=JSON.parse(value);return Array.isArray(a)&&a.length===6&&uuid(a[0])&&integer(a[1])&&a[1]>0&&decimal(a[2])&&integer(a[3])&&a[3]>0&&decimal(a[4])&&typeof a[5]==='boolean';}catch{return false;}
  }
  function packet(data,source){
    need(object(data)&&uuid(data.witness)&&nonce(data.nonce),'Response identity/nonce invalid');
    need(decimal(data.serverMonotonicNs)&&decimal(data.serverUtcNs),'Server clocks invalid');
    if(source==='beacon'){
      need(data.schema==='NovaCore.CpuBeacon/1'&&uuid(data.probeId)&&integer(data.pid)&&decimal(data.startUtcTicks),'CPU beacon identity invalid');return;
    }
    need(source==='primary'&&data.schema==='NovaCore.BlackoutWitness/1','Primary response schema invalid');
    need(['fixture','live','recovery'].includes(data.mode),'Response mode invalid');
    need(integer(data.localCommittedSerial)&&integer(data.localSkippedSamples),'Writer progress invalid');
    need(data.diskError===null||typeof data.diskError==='string','Writer error shape invalid');
    if(data.sample===null){need(data.mode==='recovery'||data.localCommittedSerial===0,'Missing active sample');return;}
    const s=data.sample,o=s?.observation;
    need(object(s)&&s.schema==='NovaCore.BlackoutWitness/1'&&s.witness===data.witness&&integer(s.serial)&&s.serial>0,'Sample identity invalid');
    need(decimal(s.sampleMonotonicNs)&&decimal(s.utcNs)&&object(o),'Sample clock/observation invalid');
    if(o.status){need(['waiting-for-new-session','sample-error'].includes(o.status),'Unknown observation status');return;}
    need(identity(o)!==null&&typeof o.fixture==='boolean','Recorder incarnation invalid');
    for(const field of ['qpc','producedBefore','producedAfter','headerProduced','consumed','durable','observerHeartbeat','producerHeartbeat','producerFault','observerFault','dropped','done'])need(decimal(o[field]),'Invalid exact field '+field);
    need(signed(o.ready),'Invalid exact ready field');
    need(integer(o.qpcFrequency)&&o.qpcFrequency>0,'QPC frequency invalid');
    for(const field of ['producerProcess','observerProcess'])need(['alive','exited','query-failed'].includes(o[field]),'Process state invalid');
    need(typeof o.eventValid==='boolean'&&typeof o.headerWithinBracket==='boolean'&&typeof o.stableProduced==='boolean','Observation flags invalid');
    if(o.eventValid){
      need(Array.isArray(o.latestEventWords)&&o.latestEventWords.length===30&&o.latestEventWords.every(decimal),'Event words invalid');
      need(typeof o.latestEventBase64==='string'&&o.latestEventBase64.length===344,'Raw event size invalid');
      const raw=atob(o.latestEventBase64);need(raw.length===256,'Raw event decoded size invalid');
      const word=offset=>{let n=0n;for(let i=7;i>=0;i--)n=(n<<8n)|BigInt(raw.charCodeAt(offset+i));return n;};
      let hash=14695981039346656037n;for(let i=0;i<240;i++)hash=BigInt.asUintN(64,(hash^BigInt(raw.charCodeAt(i)))*1099511628211n);
      need(word(240)===hash&&word(0)===BigInt(o.producedBefore)&&word(248)===word(0),'Raw event checksum/commit mismatch');
      need(o.latestEventWords.every((v,i)=>BigInt(v)===word(i*8)),'Raw event/words disagree');
    }
  }
  function row(value){
    need(object(value)&&typeof value.kind==='string'&&hex(value.pageIncarnation)&&integer(value.sequence)&&value.sequence>0,'Row identity invalid');
    if(value.kind==='response'||value.kind==='request-failure'){
      need(['primary','beacon'].includes(value.source)&&nonce(value.nonce),'Request source/nonce invalid');
      for(const key of ['sendMono','sendWall','receiveMono','receiveWall'])need(finite(value[key]),'Request timing invalid');
      need(value.receiveMono>=value.sendMono&&integer(value.generation),'Request ordering invalid');
      need(value.markerId===null||hex(value.markerId),'Request marker invalid');
      if(value.kind==='response'){
        packet(value.data,value.source);need(value.data.nonce===value.nonce,'Echoed nonce mismatch');
        need(typeof value.accepted==='boolean','Reply admission missing');
      }
    }else need(finite(value.phoneMono)&&finite(value.phoneWall),'Page event clock invalid');
  }
  function marker(mark){
    need(object(mark)&&hex(mark.id)&&hex(mark.pageIncarnation)&&uuid(mark.witness)&&integer(mark.generation)&&mark.generation>0,'Marker identity invalid');
    need(finite(mark.phoneMono)&&finite(mark.phoneWall)&&object(mark.proofs),'Marker timing/proofs invalid');
    need(mark.binding===null||binding(mark.binding),'Marker binding invalid');
    for(const source of Object.keys(mark.proofs)){
      need(['primary','beacon'].includes(source),'Unknown marker proof');row(mark.proofs[source]);
      need(mark.proofs[source].source===source&&postMarker(mark,mark.proofs[source]),'Invalid preserved post-marker proof');
    }
  }
  function postMarker(mark,response){
    return response.kind==='response'&&response.accepted===true&&response.visible==='visible'
      &&response.pageIncarnation===mark.pageIncarnation&&response.markerId===mark.id&&response.generation===mark.generation
      &&response.sendMono>mark.phoneMono&&response.receiveMono-response.sendMono<=REPLY_MS
      &&response.nonce===response.data.nonce&&response.data.witness===mark.witness;
  }
  function create(witness,page,build,wall){
    need(uuid(witness)&&hex(page),'New capture identity invalid');
    return {schema:SCHEMA,witness,pageIncarnation:page,receiverBuild:build,createdWall:wall,rows:[],marks:[],preserved:{},pinsRevision:0,dropped:0,nextSequence:1,storageReady:false,storageError:null};
  }
  function validate(value){
    need(object(value)&&value.schema===SCHEMA&&uuid(value.witness)&&hex(value.pageIncarnation),'Capture identity invalid');
    need(Array.isArray(value.rows)&&value.rows.length<=LIMIT&&Array.isArray(value.marks)&&value.marks.length<=16&&object(value.preserved),'Capture collections invalid');
    need(integer(value.dropped)&&integer(value.nextSequence)&&value.nextSequence>0&&finite(value.createdWall),'Capture counters invalid');
    need(typeof value.receiverBuild==='string'&&/^[a-f0-9]{64}$/.test(value.receiverBuild),'Capture receiver build invalid');
    need(value.sessionBinding===null||binding(value.sessionBinding),'Capture session binding invalid');
    need(beaconIdentity(value.beaconIdentity)!==null&&value.beaconIdentity.witness===value.witness,'Capture CPU binding invalid');
    const ids=new Map(),nonces=new Map();
    function check(r){
      row(r);need(r.pageIncarnation===value.pageIncarnation&&r.sequence<value.nextSequence,'Foreign page/sequence');
      const previous=ids.get(r.sequence),text=JSON.stringify(r);need(!previous||previous===text,'Conflicting row identity');ids.set(r.sequence,text);
      if(r.kind==='response'){
        need(r.data.witness===value.witness,'Foreign response witness');
        if(r.source==='beacon')need(beaconIdentity(r.data)===beaconIdentity(value.beaconIdentity),'Foreign CPU incarnation');
        else{
          need(r.data.receiverBuild===value.receiverBuild&&beaconIdentity(r.data.beacon)===beaconIdentity(value.beaconIdentity),'Foreign response build/CPU binding');
          const observed=identity(r.data.sample?.observation);if(observed)need(observed===value.sessionBinding,'Foreign recorder incarnation');
        }
        const n=r.source+':'+r.nonce;need(!nonces.has(n)||nonces.get(n)===r.sequence,'Nonce reused across requests');nonces.set(n,r.sequence);
      }
    }
    for(const r of value.rows)check(r);
    const markerIds=new Set(),generations=new Set();
    for(const m of value.marks){marker(m);need(m.witness===value.witness&&m.pageIncarnation===value.pageIncarnation,'Foreign marker');need(m.binding===null||m.binding===value.sessionBinding,'Foreign marker session');need(!markerIds.has(m.id)&&!generations.has(m.generation),'Repeated marker identity');markerIds.add(m.id);generations.add(m.generation);for(const r of Object.values(m.proofs))check(r);}
    for(const [key,p] of Object.entries(value.preserved)){
      need(['firstFailure','blackout'].includes(key)&&object(p)&&Array.isArray(p.prelude)&&p.prelude.length<=120,'Preserved window invalid');
      need(JSON.stringify(p.prelude).length<=PRELUDE,'Preserved window oversized');for(const r of p.prelude)check(r);
      if(p.failure)check(p.failure);
    }
    need(JSON.stringify(value).length<=BUDGET,'Capture character budget exceeded');return value;
  }
  function prelude(rows){
    const result=[];let size=2;
    for(let i=rows.length-1;i>=0&&result.length<120;i--){const length=JSON.stringify(rows[i]).length+1;if(size+length>PRELUDE)break;result.unshift(copy(rows[i]));size+=length;}
    return result;
  }
  const rowSizes=new WeakMap(),pinSizes=new WeakMap();
  function trim(capture){
    const {rows,marks,preserved,...base}=capture;
    let pins=pinSizes.get(capture);
    if(!pins||pins.revision!==capture.pinsRevision){pins={revision:capture.pinsRevision,size:JSON.stringify(marks).length-2+JSON.stringify(preserved).length-2};pinSizes.set(capture,pins);}
    const length=r=>{let n=rowSizes.get(r);if(n===undefined){n=JSON.stringify(r).length;rowSizes.set(r,n);}return n;};
    let size=JSON.stringify({...base,rows:[],marks:[],preserved:{}}).length+pins.size+rows.reduce((n,r)=>n+length(r),0)+Math.max(0,rows.length-1);
    // Reserve room for status/export metadata and growing counters.
    while((rows.length>LIMIT||size>BUDGET-512)&&rows.length){size-=length(rows.shift())+(rows.length?1:0);capture.dropped++;}
    need(size<=BUDGET-512,'Pinned evidence exceeds storage budget');
  }
  function append(capture,value){
    const r={...value,pageIncarnation:capture.pageIncarnation,sequence:capture.nextSequence++};row(r);
    if(r.kind==='request-failure'&&!capture.preserved.firstFailure&&capture.rows.some(v=>v.kind==='response')){capture.preserved.firstFailure={prelude:prelude(capture.rows),failure:copy(r)};capture.pinsRevision++;}
    capture.rows.push(r);
    if(r.kind==='response')for(const mark of capture.marks)if(postMarker(mark,r)&&!mark.proofs[r.source]){mark.proofs[r.source]=copy(r);capture.pinsRevision++;}
    trim(capture);return r;
  }
  function addMarker(capture,mark){
    need(capture.marks.length<16,'Marker limit reached; preserve/export existing capture');marker(mark);
    need(mark.witness===capture.witness&&mark.pageIncarnation===capture.pageIncarnation,'Foreign marker');
    if(!capture.preserved.blackout)capture.preserved.blackout={prelude:prelude(capture.rows)};
    capture.marks.push(copy(mark));capture.pinsRevision++;trim(capture);
  }
  function health(data,previous,build){
    try{
      packet(data,'primary');need(data.receiverBuild===build,'Receiver/server build changed');
      need(data.mode!=='recovery'&&data.sample!==null,'No active sampler');
      const s=data.sample,now=BigInt(data.serverMonotonicNs);
      need(now>=BigInt(s.sampleMonotonicNs)&&now-BigInt(s.sampleMonotonicNs)<=BigInt(FRESH_MS)*1000000n,'Sampler stale');
      need(data.diskError===null&&data.committedMonotonicNs!==null&&data.localCommittedSerial>0,'Local writer unavailable');
      need(now>=BigInt(data.committedMonotonicNs)&&now-BigInt(data.committedMonotonicNs)<=BigInt(FRESH_MS)*1000000n,'Local writer stale');
      need(data.remainingSeconds===null||data.remainingSeconds>=30,'Capture nearing scheduled end');
      const o=s.observation;
      if(o.status==='waiting-for-new-session'){
        need(data.preflight?.capacitySnapshotPass===true,'Capacity snapshot unavailable');
        need(now>=BigInt(data.preflight.monotonicNs)&&now-BigInt(data.preflight.monotonicNs)<=60000000000n,'Capacity snapshot older than 60 seconds; restart witness before startup');
        return {level:'READY TO LAUNCH',reason:'Recorder reservation and exact session are checked after startup',binding:null};
      }
      need(!o.status,'Source observation error');
      const binding=identity(o);need(binding!==null,'Session not bound');
      need(o.admissionVerified===true&&o.reservedBytes==='87138304','Recorder admission not verified');
      need(o.ready==='1'&&o.done==='0'&&o.producerFault==='0'&&o.observerFault==='0'&&o.dropped==='0','Recorder fault, drop, or closed session');
      need(o.producerProcess==='alive'&&o.observerProcess==='alive','Recorder process exited/unavailable');
      need(o.headerWithinBracket&&o.eventValid&&BigInt(o.producedBefore)>0n&&BigInt(o.durable)>0n,'No valid bracketed event / first recorder commit');
      need(BigInt(o.consumed)<=BigInt(o.producedAfter)&&BigInt(o.durable)<=BigInt(o.producedAfter),'Invalid recorder watermark');
      const qpc=BigInt(o.qpc),beat=BigInt(o.observerHeartbeat);
      need(qpc>=beat&&qpc-beat<=BigInt(o.qpcFrequency)*3n,'Observer heartbeat stale');
      need(previous&&previous.witness===data.witness&&identity(previous.sample?.observation)===binding,'Awaiting two bound samples');
      need(s.serial>previous.sample.serial&&BigInt(o.observerHeartbeat)>BigInt(previous.sample.observation.observerHeartbeat),'Awaiting observer progress');
      need(data.committedSession===o.session&&data.localCommittedSerial>=previous.sample.serial,'No recent committed sample from this session');
      need(BigInt(o.producedAfter)>=BigInt(previous.sample.observation.producedAfter)&&BigInt(o.durable)>=BigInt(previous.sample.observation.durable),'Recorder watermark regressed');
      return {level:o.fixture?'FIXTURE ARMED':'ARMED',reason:o.fixture?'Synthetic CPU data; no NovaCore launch':'Exact live session and healthy witness',binding};
    }catch(error){return {level:'CONNECTED',reason:String(error.message||error),binding:null};}
  }
  return {SCHEMA,BUDGET,PRELUDE,LIMIT,FRESH_MS,REPLY_MS,object,finite,integer,hex,uuid,decimal,nonce,copy,identity,packet,row,marker,postMarker,create,validate,prelude,trim,append,addMarker,health};
});
