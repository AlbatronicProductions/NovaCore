/* Browser effects; all evidence admission rules live in receiver_core.js. */
'use strict';
const C=window.WitnessCore,base=location.pathname,build=window.NC_BUILD,$=id=>document.getElementById(id);
const randomId=()=>Array.from(crypto.getRandomValues(new Uint8Array(16)),b=>b.toString(16).padStart(2,'0')).join('');
let page=randomId(),capture=null,key=null,running=true,newRun=null,latest=null,previous=null,expectedBeacon=null;
let storageOK=false,storageError=null,generation=0,activeMark=null,exportUrl=null,lastTick=performance.now(),lastArmed=null,phoneGap=true;
const store=new window.WitnessStore.Store();
let writer=null,lastCommit=null,committedPins=-1,admissionError=null,exporting=false,starting=false;
const channels={primary:{count:0,lastReceive:null,epoch:0,timer:null,controller:null},beacon:{count:0,lastReceive:null,epoch:0,timer:null,controller:null}};
function save(){
  if(!capture)return;
  try{
    capture.storageReady=true;capture.storageError=null;C.trim(capture);
    if(!writer)throw Error('No admitted storage slot');
    writer.submit(capture,performance.now());
  }catch(error){storageOK=false;storageError=String(error);capture.storageReady=false;capture.storageError=storageError;}
}
function append(value){if(!capture)return;try{C.append(capture,value);save();}catch(error){storageOK=false;storageError=String(error);}}
function event(kind,extra={}){if(capture&&running&&!newRun&&!starting)append({kind,phoneWall:Date.now(),phoneMono:performance.now(),...extra});}
function cancel(source){const ch=channels[source];ch.epoch++;clearTimeout(ch.timer);ch.timer=null;if(ch.controller)ch.controller.abort();ch.controller=null;}
async function begin(data){
  if(starting||exporting)throw Error('Capture transition already in progress');starting=true;
  for(const source of Object.keys(channels)){cancel(source);channels[source].count=0;channels[source].lastReceive=null;}
  if(capture)page=randomId();capture=C.create(data.witness,page,build,Date.now());
  capture.userAgent=navigator.userAgent;capture.origin=location.origin;
  capture.beaconIdentity=data.beacon;capture.sessionBinding=null;
  key=null;writer=null;storageOK=false;lastCommit=null;committedPins=-1;admissionError=null;
  // Never restore into a writable capture, including after a refresh or server restart.
  running=true;newRun=null;latest=null;previous=null;expectedBeacon=data.beacon;generation=0;activeMark=null;phoneGap=true;lastArmed=null;
  $('start').hidden=true;
  try{
    key=await store.admit(capture);const ownerPage=page;
    writer=new window.WitnessStore.Writer(store,key,page,(error,ack)=>{
      if(page!==ownerPage)return;
      storageOK=!error;storageError=error?String(error):null;if(!error){lastCommit=ack.submittedMono;committedPins=ack.pinsRevision;}
    });
    starting=false;event('page-start');await writer.idle();
  }catch(error){admissionError=String(error);storageError=admissionError;storageOK=false;running=false;throw error;}
  finally{starting=false;}
  refreshSaved();
}
function fresh(source){const t=channels[source].lastReceive;return t!==null&&performance.now()-t<=C.FRESH_MS;}
function state(){
  if(newRun)return {level:'SERVER RESTARTED',reason:'Old capture is frozen. Export it, then explicitly start a new capture.'};
  if(latest?.mode==='recovery')return {level:'RECOVERY ONLY',reason:'Use Saved captures below. Originals remain read-only.'};
  if(admissionError)return {level:'UNQUALIFIED',reason:admissionError};
  if(starting)return {level:'CONNECTING',reason:'Reserving a bounded storage slot'};
  if(exporting)return {level:'EXPORTING',reason:'Capture frozen; waiting up to three seconds for storage before creating the JSON download.'};
  if(!running)return {level:'CAPTURE STOPPED',reason:'JSON is ready. Keep this page until the download is confirmed.'};
  if(!capture)return {level:'CONNECTING',reason:'Waiting for the PC witness.'};
  if(!storageOK||lastCommit===null||performance.now()-lastCommit>C.FRESH_MS)return {level:'UNQUALIFIED',reason:storageError||'Storage commit/readback pending or stale'};
  if(document.visibilityState!=='visible')return {level:'NOT ARMED',reason:'Keep this page visible and the phone awake.'};
  if(!fresh('primary'))return {level:fresh('beacon')?'HOST CPU RESPONDING':'RESPONSES STALE',reason:fresh('beacon')?'Independent CPU replies continue; main witness is silent.':'Network, phone scheduling, host, OS and power remain possible.'};
  if(!fresh('beacon'))return {level:'CONNECTED',reason:'Waiting for the independent CPU responder.'};
  if(phoneGap)return {level:'CONNECTED',reason:'Waiting for two fresh samples after page activation/gap.'};
  return C.health(latest,previous,build);
}
function render(){
  let s=state();
  if(['ARMED','FIXTURE ARMED'].includes(s.level)&&lastArmed!==s.binding){
    lastArmed=s.binding;event('armed',{binding:s.binding,sampleSerial:latest.sample.serial,committedSerial:latest.localCommittedSerial,beacon:expectedBeacon});s=state();
  }
  $('status').textContent=s.level;$('reason').textContent=s.reason;
  $('status').className=['ARMED','FIXTURE ARMED','READY TO LAUNCH'].includes(s.level)?'good':'warning';
  for(const source of Object.keys(channels)){
    $(source+'-count').textContent=String(channels[source].count)+(fresh(source)?' · fresh':' · stale');
    const m=capture?.marks.at(-1),rows=m?capture.rows.filter(r=>r.source===source&&C.postMarker(m,r)):[];
    $('post-'+source).textContent=String(Math.max(rows.length,m?.proofs[source]?1:0))+(m?.proofs[source]?(storageOK&&committedPins>=capture.pinsRevision?' · proof saved':' · proof pending storage'):'');
  }
  const o=latest?.sample?.observation;
  $('binding').textContent='Witness '+(capture?.witness||'—')+' | session '+(o?.session||'unbound')+' | page '+page;
  $('progress').textContent=o?.session?'Produced '+o.producedAfter+' / durable '+o.durable+' | observer '+o.observerProcess+' | sample '+latest.sample.serial+' / local commit '+latest.localCommittedSerial:(o?.status||'');
  $('storage').textContent='Storage '+(storageOK&&lastCommit!==null&&performance.now()-lastCommit<=C.FRESH_MS?'readback OK':'UNQUALIFIED')+' | bounded slot / 8 | retained '+(capture?.rows.length||0)+' | overwritten '+(capture?.dropped||0);
  $('mark').disabled=!capture||!running||!!newRun||starting||exporting;$('export').disabled=!capture||starting||exporting;
  $('details').textContent=JSON.stringify({state:s,storageError,last:latest},null,2);
}
async function poll(source){
  if(!running||newRun||starting||(source==='beacon'&&!expectedBeacon))return;
  const ch=channels[source],epoch=++ch.epoch,requestPage=page,config=expectedBeacon?C.copy(expectedBeacon):null;
  const nonce=randomId()+randomId(),sendMono=performance.now();
  if(activeMark&&sendMono<=activeMark.phoneMono){ch.timer=setTimeout(()=>poll(source),1);return;}
  const request={source,nonce,sendMono,sendWall:Date.now(),generation,markerId:activeMark?.id||null};
  const controller=new AbortController();ch.controller=controller;const timeout=setTimeout(()=>controller.abort(),C.REPLY_MS);
  let acceptedEpoch=epoch;
  try{
    const response=await fetch(source==='primary'?base+'sample/'+nonce:config.url+'sample/'+nonce,{cache:'no-store',signal:controller.signal});
    if(!response.ok)throw Error('HTTP '+response.status);
    const text=await response.text();if(text.length>32768)throw Error('Oversized response');
    const data=JSON.parse(text);C.packet(data,source);if(data.nonce!==nonce)throw Error('Nonce mismatch');
    if(!running||newRun)return;
    if(requestPage!==page)return;
    if(epoch!==ch.epoch){
      // A cancelled first-bound response must not establish a session or poison
      // a still-unbound archive. Keep the rejection chronology, never a proof.
      if(capture&&data.witness===capture.witness)event('cancelled-request-reply',{source,nonce,sendMono,requestGeneration:request.generation,requestMarkerId:request.markerId,replyWitness:data.witness});
      return;
    }
    if(source==='primary'&&data.receiverBuild!==build)throw Error('Receiver build changed; export and reload');
    if(source==='primary'&&data.mode==='recovery'&&!capture){latest=data;refreshSaved();return;}
    if(!capture){
      if(epoch!==ch.epoch)return;
      await begin(data);acceptedEpoch=ch.epoch;request.generation=0;request.markerId=null;
      if(!running||newRun||requestPage!==page)return;
    }
    if(data.witness!==capture.witness){
      if(source==='primary'){
        event('server-replaced',{newWitness:data.witness});newRun=data;
        for(const name of Object.keys(channels)){cancel(name);channels[name].lastReceive=null;}
        $('start').hidden=false;refreshSaved();return;
      }
      throw Error('Foreign CPU witness');
    }
    if(source==='beacon'&&(!config||data.probeId!==config.probeId||data.pid!==config.pid||data.startUtcTicks!==config.startUtcTicks))throw Error('CPU process incarnation mismatch');
    if(source==='primary'&&expectedBeacon&&JSON.stringify(data.beacon)!==JSON.stringify(expectedBeacon))throw Error('CPU identity changed within witness');
    // A cancelled request may finish after the marker. Keep it rejected; it cannot prove post-marker execution.
    if(requestPage!==page)return;
    if(source==='primary'){
      const binding=C.identity(data.sample?.observation);
      if(binding&&capture.sessionBinding&&capture.sessionBinding!==binding)throw Error('Recorder session/incarnation changed within witness');
      if(binding&&!capture.sessionBinding)capture.sessionBinding=binding;
    }
    const receiveMono=performance.now(),accepted=acceptedEpoch===ch.epoch&&receiveMono-sendMono<=C.REPLY_MS&&document.visibilityState==='visible';
    append({kind:'response',...request,receiveMono,receiveWall:Date.now(),visible:document.visibilityState,accepted,data});
    if(accepted){
      ch.count++;ch.lastReceive=receiveMono;
      if(source==='primary'){
        if(latest?.sample&&data.sample&&data.sample.serial!==latest.sample.serial)previous=latest;
        latest=data;
        if(previous&&previous.sample.serial<data.sample?.serial)phoneGap=false;
        if(!channels.beacon.controller&&!channels.beacon.timer)channels.beacon.timer=setTimeout(()=>poll('beacon'),0);
      }
    }
  }catch(error){
    if(running&&!newRun&&acceptedEpoch===ch.epoch){ch.lastReceive=null;if(capture)append({kind:'request-failure',...request,receiveMono:performance.now(),receiveWall:Date.now(),visible:document.visibilityState,error:String(error)});}
  }finally{
    clearTimeout(timeout);
    if(acceptedEpoch===ch.epoch){ch.controller=null;if(running&&!newRun){const stress=source==='primary'&&location.search==='?stress=1'&&latest?.mode==='fixture';ch.timer=setTimeout(()=>poll(source),stress?100:1000);}}
    render();
  }
}
$('mark').onclick=()=>{
  if(!capture||!running||newRun||starting||exporting)return;
  try{
    const mark={id:randomId(),pageIncarnation:page,witness:capture.witness,generation:++generation,phoneWall:Date.now(),phoneMono:performance.now(),binding:C.identity(latest?.sample?.observation),proofs:{}};
    C.addMarker(capture,mark);activeMark=mark;save();
    for(const source of Object.keys(channels)){cancel(source);channels[source].timer=setTimeout(()=>poll(source),1);}
  }catch(error){storageOK=false;storageError=String(error);}
  render();
};
function download(text,name){
  if(exportUrl)URL.revokeObjectURL(exportUrl);
  exportUrl=URL.createObjectURL(new Blob([text],{type:'application/json'}));
  const link=$('download-link');link.href=exportUrl;link.download=name;
  $('open-link').href=exportUrl;$('export-text').value=text;$('export-panel').hidden=false;link.click();
}
$('export').onclick=async()=>{
  if(!capture||exporting||starting)return;exporting=true;
  const target=capture,targetWriter=writer,targetPage=page;
  if(running&&!newRun){event('capture-stopped',{reason:'operator-export'});running=false;for(const source of Object.keys(channels))cancel(source);}
  try{
    if(targetWriter&&!targetWriter.closing){let timeout;try{
      await Promise.race([targetWriter.seal(target,performance.now()),new Promise((_,reject)=>{timeout=setTimeout(()=>reject(Error('Storage sealing timed out; exported frozen RAM evidence, stored copy remains protected')),3000);})]);
    }catch(error){storageOK=false;storageError=String(error);}finally{clearTimeout(timeout);}}
    const value=C.copy(target);value.exportWall=Date.now();value.storageReady=storageOK;value.storageError=storageError;
    download(JSON.stringify(value,null,2),'novacore-phone-witness-'+target.witness+'-'+targetPage+'.json');render();refreshSaved();
  }finally{exporting=false;render();}
};
async function refreshSaved(){
  const select=$('saved'),selected=select.value;select.replaceChildren();let invalid=0,count=0;const errors=[];
  try{
    for(const r of await store.list()){
      const option=document.createElement('option');option.value='idb:'+r.key;
      option.textContent=(r.sealed?'STOPPED':'PROTECTED / unsealed')+' — '+r.key;select.appendChild(option);count++;
    }
  }catch(error){errors.push('IndexedDB unavailable: '+String(error));}
  try{
    for(let i=0;i<localStorage.length;i++){
      const savedKey=localStorage.key(i);if(!savedKey||(!savedKey.startsWith('nc-blackout:')&&!savedKey.startsWith('nc-blackout-v2:')))continue;
      let label;
      try{
        const text=localStorage.getItem(savedKey);if(text.length>8*1024*1024)throw Error('Oversized');const value=JSON.parse(text);
        if(value.schema===C.SCHEMA){C.validate(value);label=value.witness+' / '+value.pageIncarnation+' / marks '+value.marks.length;}
        else if(value.schema==='NovaCore.PhoneWitness/1')label='LEGACY V1 — '+value.witness+' (raw export only)';
        else throw Error('Unknown schema');
      }catch(error){invalid++;label='INVALID — original preserved: '+savedKey;}
      const option=document.createElement('option');option.value=savedKey;option.textContent=label;select.appendChild(option);count++;
    }
  }catch(error){errors.push('Legacy storage unavailable: '+String(error));}
  if([...select.options].some(o=>o.value===selected))select.value=selected;
  $('recovery-status').textContent=count+' saved captures; '+invalid+' invalid. Old localStorage is read-only. New captures use 8 bounded slots. Verify a downloaded JSON below to retire only its matching stopped IndexedDB copy. '+errors.join('; ');
}
$('recover-export').onclick=async()=>{try{const selected=$('saved').value;const text=selected.startsWith('idb:')?JSON.stringify((await store.load(selected.slice(4))).value,null,2):localStorage.getItem(selected);if(text===null)throw Error('Selection unavailable');download(text,'novacore-phone-recovered-'+Date.now()+'.json');}catch(error){$('recovery-status').textContent=String(error);}};
$('verify-file').onchange=async e=>{
  // Android providers may report JSON as text/plain, octet-stream, or no MIME.
  // Snapshot Files before the first await; never clear/replace the picker on return.
  const input=e.currentTarget||e.target,files=Array.from(input.files||[]);
  if(!files.length)return;
  $('selected-files').textContent='Selected: '+files.map(file=>file.name).join(', ');
  $('retire-status').textContent='Verifying file contents and exact saved capture identity…';
  input.disabled=true;
  const messages=[];
  try{
    for(const file of files){
      try{if(file.size>8*1024*1024)throw Error('File too large');const retired=await store.retire(await file.text());messages.push('Verified and retired saved copy: '+retired+' — '+file.name+'. Downloaded file remains unchanged.');}
      catch(error){messages.push(file.name+': '+String(error)+' — no matching saved copy retired.');}
      $('retire-status').textContent=messages.join('\n');
    }
  }finally{input.disabled=false;}
  refreshSaved();
};
$('seal-saved').onclick=async()=>{
  try{const selected=$('saved').value;if(!selected.startsWith('idb:'))throw Error('Legacy originals remain read-only');
    if(selected==='idb:'+key)throw Error('Use EXPORT JSON to stop this current capture');
    if(!$('closed-original').checked)throw Error('Close the original page first, then check the confirmation');
    await store.sealAbandoned(selected.slice(4));$('closed-original').checked=false;await refreshSaved();$('retire-status').textContent='Saved snapshot frozen. Export it, then select that downloaded JSON to verify and retire.';
  }catch(error){$('retire-status').textContent=String(error);}
};
$('start').onclick=async()=>{if(!newRun||exporting||starting)return;const data=newRun;try{await begin(data);channels.primary.timer=setTimeout(()=>poll('primary'),0);}catch(_){}render();};
function resetFreshness(){previous=null;latest=null;phoneGap=true;lastArmed=null;for(const source of Object.keys(channels))channels[source].lastReceive=null;}
document.addEventListener('visibilitychange',()=>{
  event('visibility',{state:document.visibilityState});resetFreshness();
  if(running&&!newRun&&document.visibilityState==='visible')for(const source of Object.keys(channels)){cancel(source);channels[source].timer=setTimeout(()=>poll(source),0);}
  render();
});
window.addEventListener('pagehide',()=>{event('pagehide');resetFreshness();});
window.addEventListener('pageshow',()=>{event('pageshow');resetFreshness();});
setInterval(()=>{const now=performance.now();if(now-lastTick>4000){event('phone-scheduling-gap',{milliseconds:now-lastTick});resetFreshness();}lastTick=now;render();},250);
refreshSaved();poll('primary');
