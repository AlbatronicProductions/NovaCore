// CPU-only deterministic page/evidence qualification. No Android or GPU claims.
const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
const C=require('./receiver_core.js'),crypto=require('node:crypto');
const app=fs.readFileSync(__dirname+'/receiver_app.js','utf8');
const W='11111111-1111-4111-8111-111111111111',W2='22222222-2222-4222-8222-222222222222',S='33333333-3333-4333-8333-333333333333',P='44444444-4444-4444-8444-444444444444',B='a'.repeat(64);
const checks=[];const check=(name,fn)=>{fn();checks.push(name)};
function rawEvent(sequence,qpc){
  const b=Buffer.alloc(256);b.writeBigUInt64LE(BigInt(sequence),0);b.writeBigUInt64LE(BigInt(qpc),8);b.writeBigUInt64LE(8n,32);b.writeBigUInt64LE(1n,40);b.writeBigUInt64LE(18446744073709551615n,152);
  let h=14695981039346656037n;for(const byte of b.subarray(0,240))h=BigInt.asUintN(64,(h^BigInt(byte))*1099511628211n);
  b.writeBigUInt64LE(h,240);b.writeBigUInt64LE(BigInt(sequence),248);return b;
}
function packet(nonce,t,serial=1,witness=W){
  const qpc=String(Math.floor(t*10000)),raw=rawEvent(serial,qpc),beacon={schema:'NovaCore.CpuBeacon/1',witness,probeId:P,pid:2,startUtcTicks:'639261612520224860',port:58762,url:'http://fixture:58762/token/'};
  return {schema:'NovaCore.BlackoutWitness/1',witness,nonce,serverMonotonicNs:String(Math.floor(t*1e6)),serverUtcNs:'1790000000000000000',mode:'fixture',localCommittedSerial:serial,localSkippedSamples:0,committedSession:S,committedMonotonicNs:String(Math.floor(t*1e6)),diskError:null,receiverBuild:B,remainingSeconds:null,beacon,
    sample:{schema:'NovaCore.BlackoutWitness/1',witness,serial,sampleMonotonicNs:String(Math.floor(t*1e6)),utcNs:'1790000000000000000',observation:{fixture:true,session:S,producerPid:1,observerPid:2,producerStartUtcTicks:'639261612505478296',observerStartUtcTicks:'639261612520224860',qpc,qpcFrequency:10000000,producedBefore:String(serial),producedAfter:String(serial),headerProduced:String(serial),consumed:String(serial),durable:String(serial),observerHeartbeat:qpc,producerHeartbeat:qpc,producerProcess:'alive',observerProcess:'alive',producerFault:'0',observerFault:'0',ready:'1',done:'0',dropped:'0',headerWithinBracket:true,stableProduced:true,eventValid:true,admissionVerified:true,reservedBytes:'87138304',latestEventBase64:raw.toString('base64'),latestEventWords:Array.from({length:30},(_,i)=>raw.readBigUInt64LE(i*8).toString())}}};
}
function harness(saved=new Map(),options={}){
  const elements={},events={},winEvents={},blobs=[],timers=new Map();let timer=0,clock=1000,serial=0,quota=false,readback=false,offline=false,witness=W,mode=options.mode||'fixture',hold=false,pending=[],mutate=options.mutate||null;
  // Effects harness isolates admission/proof logic; real IndexedDB is exercised
  // separately by test_standing.cjs in Chromium.
  const revisions=new Map(),sealed=new Set();
  const storage={
    Store:class {
      async admit(v){if(options.holdAdmit)await new Promise(r=>options.releaseAdmit=r);const k=v.witness+':'+v.pageIncarnation; saved.set(k,JSON.stringify(v));revisions.set(k,0);return k;}
      snapshot(v){return C.copy(v)}
      async save(k,o,r,v,seal=false){if(quota)throw Error('QuotaExceeded');if(readback)throw Error('Storage readback mismatch');if(sealed.has(k))throw Error('sealed');saved.set(k,JSON.stringify(v));if(seal)sealed.add(k);revisions.set(k,r+1);return r+1;}
      async list(){return [...saved].filter(([k])=>!k.startsWith('nc-blackout')).map(([key])=>({key,sealed:sealed.has(key)}));}
      async load(k){return {value:JSON.parse(saved.get(k))};}
    }
  };
  const storeContext={WitnessCore:C};vm.runInNewContext(fs.readFileSync(__dirname+'/receiver_store.js','utf8'),storeContext);storage.Writer=storeContext.WitnessStore.Writer;
  const node=()=>({value:'',options:[],hidden:false,disabled:false,textContent:'',click(){},replaceChildren(){this.options=[]},appendChild(o){this.options.push(o);if(!this.value)this.value=o.value}});
  const context=vm.createContext({
    location:{pathname:'/token/',origin:'http://fixture:58761',search:''},navigator:{userAgent:'CPU VM'},performance:{now:()=>++clock},Date,JSON,BigInt,AbortController,Blob,Uint8Array,crypto:crypto.webcrypto,
    URL:{createObjectURL:b=>{blobs.push(b);return 'blob:test'},revokeObjectURL:()=>{}},
    setTimeout:(fn,ms)=>{timers.set(++timer,{fn,ms});return timer},clearTimeout:id=>timers.delete(id),setInterval:()=>++timer,
    localStorage:{get length(){return saved.size},key:i=>[...saved.keys()][i]??null,getItem:k=>readback?'mismatch':saved.get(k)??null,setItem:(k,v)=>{if(quota)throw Error('QuotaExceeded');saved.set(k,v)}},
    document:{visibilityState:'visible',getElementById:id=>elements[id]??=(node()),createElement:node,addEventListener:(name,fn)=>events[name]=fn},window:{WitnessCore:C,WitnessStore:storage,NC_BUILD:B,addEventListener:(name,fn)=>winEvents[name]=fn},
    fetch:async url=>{
      if(offline)throw Error('Synthetic disconnect');
      const source=url.startsWith('http:')?'beacon':'primary',nonce=url.split('/').at(-1);
      const answer=()=>{let data=packet(nonce,++clock,++serial,witness);if(source==='beacon')data={...data.beacon,nonce,serverMonotonicNs:data.serverMonotonicNs,serverUtcNs:data.serverUtcNs};else if(mode==='recovery')data={...data,mode,sample:null,localCommittedSerial:0};if(mutate)data=mutate(data,source);return {ok:true,text:async()=>JSON.stringify(data)}};
      if(hold){const generated=answer();return await new Promise(resolve=>pending.push(()=>resolve(generated)))}return answer();
    }
  });
  const run=s=>vm.runInContext(s,context);run(app);
  return {run,elements,saved,blobs,events,winEvents,context,timers,setOffline:v=>offline=v,setQuota:v=>quota=v,setReadback:v=>readback=v,setHold:v=>hold=v,setMode:v=>mode=v,setWitness:v=>witness=v,setMutate:v=>mutate=v,advance:n=>clock+=n,release:()=>pending.shift()(),poll:async s=>{await run('poll('+JSON.stringify(s)+')');await run('writer?.idle()');}};
}
const tick=()=>new Promise(r=>setImmediate(r));
(async()=>{
  let h=harness();await tick();await h.poll('beacon');await h.poll('primary');
  check('two fresh bound samples and independent CPU required for FIXTURE ARMED',()=>assert.equal(h.run('state().level'),'FIXTURE ARMED'));
  check('all integer event words remain exact through JSON/storage',()=>assert.equal(h.run('capture.rows.find(r=>r.kind==="response").data.sample.observation.latestEventWords[19]'),'18446744073709551615'));
  h.setHold(true);const flight=h.poll('primary');await tick();h.elements.mark.onclick();h.release();await flight;h.setHold(false);
  check('pre-marker in-flight reply cannot satisfy marker',()=>assert.equal(h.run('Object.keys(capture.marks[0].proofs).length'),0));
  await h.poll('primary');await h.poll('beacon');
  check('fresh post-marker request/nonce proofs retained for both responders',()=>{assert.equal(h.run('Object.keys(capture.marks[0].proofs).length'),2);C.validate(JSON.parse(h.run('JSON.stringify(capture)')))});
  h.elements.mark.onclick();h.setHold(true);const delayed=h.poll('primary');await tick();h.advance(2000);h.release();await delayed;h.setHold(false);
  check('delayed matching response recorded but not admitted as fresh proof',()=>{assert.equal(h.run('Object.keys(capture.marks[1].proofs).length'),0);assert.equal(h.run('capture.rows.at(-1).accepted'),false)});
  h.setMutate((d,s)=>({...d,nonce:'b'.repeat(64)}));await h.poll('primary');h.setMutate(null);
  check('wrong nonce rejected',()=>assert.match(h.run('capture.rows.at(-1).error'),/Nonce/));
  h.setMutate((d,s)=>s==='beacon'?{...d,startUtcTicks:'9'}:d);await h.poll('beacon');h.setMutate(null);
  check('CPU process birth identity mismatch rejected',()=>assert.match(h.run('capture.rows.at(-1).error'),/incarnation/));
  h.setMutate((d,s)=>{if(s==='primary')d.sample.observation.session=W2;return d});await h.poll('primary');h.setMutate(null);
  check('same witness cannot change recorder session',()=>assert.match(h.run('capture.rows.at(-1).error'),/session\/incarnation/));
  h.setOffline(true);await h.poll('primary');await h.poll('beacon');
  check('disconnect drops freshness and pins first failure',()=>{assert.equal(h.run('state().level'),'RESPONSES STALE');assert.ok(h.run('capture.preserved.firstFailure'))});
  h.setOffline(false);await h.poll('primary');await h.poll('beacon');await h.poll('primary');
  check('reconnect requires and accepts new matching responses',()=>assert.equal(h.run('state().level'),'FIXTURE ARMED'));
  h.run('document.visibilityState="hidden"');h.events.visibilitychange();
  check('hidden page loses ARMED',()=>assert.equal(h.run('state().level'),'NOT ARMED'));
  h.run('document.visibilityState="visible"');h.events.visibilitychange();await h.poll('primary');await h.poll('beacon');
  check('foreground requires two new samples',()=>assert.equal(h.run('state().level'),'CONNECTED'));await h.poll('primary');
  await h.run('writer.idle()');const oldKey=h.run('key');const snapshot=h.saved.get(oldKey);let reopened=harness(h.saved);await tick();
  check('refresh/reopen isolates new page and preserves previous capture bytes',()=>{assert.notEqual(reopened.run('key'),oldKey);assert.equal(h.saved.get(oldKey),snapshot)});
  const malformed='{"schema":"NovaCore.PhoneWitness/2","rows":{}}';h.saved.set('nc-blackout-v2:malformed',malformed);await reopened.run('refreshSaved()');
  check('malformed restored data is read-only and visibly invalid',()=>{assert.match(reopened.elements['recovery-status'].textContent,/1 invalid/);assert.equal(h.saved.get('nc-blackout-v2:malformed'),malformed)});
  h.setWitness(W2);await h.poll('primary');await h.run('writer.idle()');const frozen=h.saved.get(oldKey);
  check('new witness freezes previous capture instead of merging',()=>assert.equal(h.run('state().level'),'SERVER RESTARTED'));
  await h.elements.start.onclick();await h.poll('primary');
  check('explicit new capture changes page/key without mutating previous',()=>{assert.notEqual(h.run('key'),oldKey);assert.equal(h.saved.get(oldKey),frozen)});
  h.elements.mark.onclick();await h.poll('primary');await h.poll('beacon');
  const proof=h.run('JSON.stringify(capture.marks[0].proofs)');
  h.run('for(let n=0;n<2100;n++)C.append(capture,{kind:"rollover",phoneWall:Date.now(),phoneMono:performance.now(),payload:"x".repeat(1800)});save()');
  check('row/character rollover retains pinned challenge proof',()=>{assert.ok(h.run('capture.dropped')>0);assert.equal(h.run('JSON.stringify(capture.marks[0].proofs)'),proof);C.validate(JSON.parse(h.run('JSON.stringify(capture)')))});
  await h.run('writer.idle()');h.setQuota(true);h.run('event("quota-test")');await h.run('writer.idle()');
  check('quota failure removes ARMED and remains visible',()=>assert.equal(h.run('state().level'),'UNQUALIFIED'));
  h.setOffline(true);await h.elements.export.onclick();const exported=JSON.parse(await h.blobs.at(-1).text());
  check('offline export retains proof and failed storage status; capture stops',()=>{assert.equal(exported.storageReady,false);assert.equal(h.run('running'),false);assert.ok(exported.marks[0].proofs.primary)});
  const originalMap=new Map(h.saved);let recovery=harness(new Map(originalMap),{mode:'recovery'});await tick();await recovery.poll('primary');
  recovery.elements.saved.value='idb:'+oldKey;await recovery.elements['recover-export'].onclick();
  check('recovery only starts no capture and exports exact original bytes without writes',()=>{assert.equal(recovery.run('capture'),null);assert.equal(recovery.run('state().level'),'RECOVERY ONLY');assert.deepEqual([...recovery.saved], [...originalMap])});assert.deepEqual(JSON.parse(await recovery.blobs.at(-1).text()),JSON.parse(frozen));
  const late=harness();await tick();late.setHold(true);const oldFlight=late.poll('primary');await tick();late.elements.mark.onclick();late.setHold(false);late.setWitness(W2);await late.poll('primary');await late.elements.start.onclick();await late.poll('primary');late.release();await oldFlight;
  check('already-generated old reply cannot replace/freeze a newer capture',()=>{assert.equal(late.run('capture.witness'),W2);assert.equal(late.run('newRun'),null)});
  const unbound=harness(new Map(),{mutate:(d,s)=>{if(s==='primary')d.sample.observation={status:'waiting-for-new-session',fixture:false};return d}});await tick();unbound.setMutate(null);unbound.setHold(true);const firstBound=unbound.poll('primary');await tick();unbound.elements.mark.onclick();unbound.release();await firstBound;await unbound.elements.export.onclick();
  check('cancelled first-bound reply preserves a valid unbound archive',()=>{const v=JSON.parse(unbound.run('JSON.stringify(capture)'));assert.equal(v.sessionBinding,null);assert.equal(Object.keys(v.marks[0].proofs).length,0);C.validate(v)});
  const good=packet('a'.repeat(64),1000,1),next=packet('b'.repeat(64),2000,2);
  check('idle producer with observer progress remains armed',()=>{next.sample.observation.producedBefore='1';next.sample.observation.producedAfter='1';next.sample.observation.durable='1';next.sample.observation.consumed='1';next.sample.observation.latestEventBase64=good.sample.observation.latestEventBase64;next.sample.observation.latestEventWords=good.sample.observation.latestEventWords;assert.equal(C.health(next,good,B).level,'FIXTURE ARMED')});
  check('stale writer, stale observer, faults and missing admission never arm',()=>{
    for(const mutate of [d=>d.diskError='disk',d=>{d.committedMonotonicNs='0';d.serverMonotonicNs='9999999999'},d=>d.sample.observation.observerHeartbeat='0',d=>d.sample.observation.ready='-1',d=>d.sample.observation.admissionVerified=false,d=>d.sample.observation.dropped='1',d=>d.sample.observation.producerStartUtcTicks='2']){const v=C.copy(next);mutate(v);if(v.sample.observation.observerHeartbeat==='0')v.sample.observation.qpc='9999999999';assert.notEqual(C.health(v,good,B).level,'FIXTURE ARMED')}
  });
  check('prelaunch capacity expires by monotonic time despite wall-clock rollback',()=>{
    const v=C.copy(next);v.sample.observation={status:'waiting-for-new-session',fixture:false};v.preflight={capacitySnapshotPass:true,monotonicNs:v.serverMonotonicNs};
    assert.equal(C.health(v,good,B).level,'READY TO LAUNCH');v.serverMonotonicNs=String(BigInt(v.serverMonotonicNs)+61000000000n);v.sample.sampleMonotonicNs=v.serverMonotonicNs;v.committedMonotonicNs=v.serverMonotonicNs;v.serverUtcNs='1';
    assert.equal(C.health(v,good,B).level,'CONNECTED');
  });
  check('malformed proof, foreign page, duplicate generation and exact-word loss rejected',()=>{
    for(const mutate of [v=>v.marks[0].proofs.primary.sendMono=v.marks[0].phoneMono,v=>v.marks[0].proofs.primary.nonce='a'.repeat(64),v=>v.marks.push(C.copy(v.marks[0])),v=>v.rows[0].pageIncarnation='f'.repeat(32),v=>v.marks[0].proofs.beacon.data.startUtcTicks='9',v=>v.marks[0].proofs.primary.data.sample.observation.session=W,v=>v.marks[0].binding='arbitrary']){const v=C.copy(exported);mutate(v);assert.throws(()=>C.validate(v))}
    const d=C.copy(good);d.sample.observation.latestEventWords[19]=18446744073709551615;assert.throws(()=>C.packet(d,'primary'));
  });
  const opts={holdAdmit:true},admitting=harness(new Map(),opts);await tick();admitting.run('render()');
  await admitting.elements.export.onclick();
  check('export cannot race an uncompleted storage admission',()=>{assert.equal(admitting.blobs.length,0);assert.equal(admitting.elements.export.disabled,true)});
  opts.releaseAdmit();await tick();await admitting.elements.export.onclick();
  check('admission completion never appends after the terminal export entry',()=>assert.equal(admitting.run('capture.rows.at(-1).kind'),'capture-stopped'));
  const switching=harness();await tick();switching.setWitness(W2);await switching.poll('primary');
  switching.run('const realSeal=writer.seal.bind(writer);writer.seal=(...args)=>new Promise(resolve=>globalThis.releaseSeal=()=>resolve(realSeal(...args)))');
  const sealing=switching.elements.export.onclick();await switching.elements.start.onclick();
  check('start-new cannot switch the target of an outstanding export',()=>assert.equal(switching.run('capture.witness'),W));
  switching.run('releaseSeal()');await sealing;assert.equal(JSON.parse(await switching.blobs.at(-1).text()).witness,W);
  const lag=harness();await tick();await lag.poll('primary');await lag.poll('beacon');
  lag.run('const realSave=store.save.bind(store);globalThis.held=[];store.save=(...args)=>new Promise((resolve,reject)=>held.push(()=>realSave(...args).then(resolve,reject)));event("old-snapshot")');
  lag.advance(10000);await lag.run('poll("primary")');await lag.run('poll("beacon")');
  await lag.run('(async()=>{const flight=writer.flight;held.shift()();await flight;})()');
  check('late old commit cannot restore ARMED while fresh pending data is uncommitted',()=>assert.equal(lag.run('state().level'),'UNQUALIFIED'));
  lag.run('store.save=realSave;held.shift()()');await lag.run('writer.idle()');
  const legacy=harness(new Map([['nc-blackout:original','original bytes']]),{mode:'recovery'});await tick();
  legacy.run('store.list=async()=>{throw Error("IndexedDB unavailable")}');await legacy.run('refreshSaved()');
  check('failed IndexedDB enumeration preserves legacy recovery selection',()=>assert.ok(legacy.elements.saved.options.some(o=>o.value==='nc-blackout:original')));
  const hung=harness();await tick();await hung.poll('primary');await hung.poll('beacon');
  hung.run('store.save=()=>new Promise(()=>{});event("hung-storage")');const waitingExport=hung.elements.export.onclick();
  check('export honestly indicates sealing in progress',()=>assert.equal(hung.run('state().level'),'EXPORTING'));
  [...hung.timers.values()].find(t=>t.ms===3000).fn();await waitingExport;
  const ram=JSON.parse(await hung.blobs.at(-1).text());
  check('hung storage still allows bounded offline RAM export with explicit unqualified status',()=>{assert.equal(ram.storageReady,false);assert.match(ram.storageError,/timed out/);assert.equal(ram.rows.at(-1).kind,'capture-stopped')});
  const result={passed:true,checks,qualification:'CPU page logic only; real S24 browser, transport and downloaded JSON still require inspection'};
  if(process.argv[2]){fs.mkdirSync(process.argv[2],{recursive:false});fs.writeFileSync(process.argv[2]+'/results.json',JSON.stringify(result,null,2),{flag:'wx'});fs.writeFileSync(process.argv[2]+'/synthetic-export.json',JSON.stringify(exported,null,2),{flag:'wx'});}
  console.log(JSON.stringify(result,null,2));
})().catch(error=>{console.error(error);process.exitCode=1});

